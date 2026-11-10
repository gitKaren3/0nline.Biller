import psycopg2
import json
import os

# Load config
base_dir = os.path.dirname(__file__)
config_path = os.path.join(base_dir, "SqlQueryGenerator.json")
with open(config_path, "r") as f:
    config = json.load(f)

output_dir = os.path.abspath(os.path.join(base_dir, config["OutputDir"]))
os.makedirs(output_dir, exist_ok=True)

ns = config["Namespace"]
modelsns = config["ModelsNamespace"]
lookupns = config.get("LookupNamespace", modelsns + ".Lookup")
connection_string = config["ConnectionString"]
scope = config["Scope"]
Depluralize = config.get("Depluralize", "false").lower() == "true"
base_class = config.get("BaseClass", "{base_class}")

# LookupLabels is APP-SPECIFIC configuration, not part of the schema or the
# shared libraries. Each app's SqlQueryGenerator.json declares its own labels
# for the tables it exposes in a picker; a table can be labelled "Jones, Harriet"
# in Biller and "Harriet" in another app. See Workspace/docs/practices.md.
#
#   "LookupLabels": {
#     "Contact": { "columns": ["Surname", "FirstName"], "separator": ", " }
#   }
lookup_labels = config.get("LookupLabels", {})

# LookupFilters narrows what a picker offers. It is app-specific too, and it is
# a raw SQL predicate ANDed to the lookup's WHERE. The configuration carries the
# domain rule ("a client list must not include my own profile"), which the
# generator cannot infer from the schema. It is passed through as-is: it is not
# made dialect-neutral, so a predicate such as IS DISTINCT FROM remains
# Postgres-only by design.
lookup_filters = config.get("LookupFilters", {})

def csharp_string(value: str) -> str:
    """Escape a value for a C# string literal emitted below."""
    return value.replace("\\", "\\\\").replace('"', '\\"')


def csharp_string_list(values) -> str:
    return "new[] { " + ", ".join(f'"{csharp_string(v)}"' for v in values) + " }"


def get_class_name(table_name: str) -> str:
    if Depluralize:
        if table_name.endswith("ies"):
            return table_name[:-3] + "y"
        elif table_name.endswith("s"):
            return table_name[:-1]
    return table_name

# Connect to Postgres
conn = psycopg2.connect(connection_string)
cur = conn.cursor()

# Get tables
cur.execute("""
    SELECT table_name, table_schema
    FROM information_schema.tables
    WHERE table_schema NOT IN ('pg_catalog','information_schema');
""")
tables = cur.fetchall()
print("Found tables:", tables)

# A misnamed table in LookupLabels or LookupFilters would otherwise be ignored silently.
table_names = [t[0] for t in tables]
unknown_tables = [t for t in lookup_labels if t not in table_names]
if unknown_tables:
    raise ValueError(f"LookupLabels names tables that do not exist: {unknown_tables}")

unknown_tables = [t for t in lookup_filters if t not in table_names]
if unknown_tables:
    raise ValueError(f"LookupFilters names tables that do not exist: {unknown_tables}")

for table_name, table_schema in tables:
    class_name = get_class_name(table_name)
    active_column = ""

    # Primary key: every column, in schema order. A composite key is a fact about
    # the table, not something the generator may collapse to its first column.
    cur.execute("""
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
          ON tc.constraint_name = kcu.constraint_name
         AND tc.table_schema = kcu.table_schema
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_name = %s
          AND tc.table_schema = %s
        ORDER BY kcu.ordinal_position;
    """, (table_name, table_schema))
    pk_columns = [row[0] for row in cur.fetchall()]
    pk_column = pk_columns[0] if pk_columns else ""

    # Columns
    cur.execute("""
        SELECT column_name, data_type, is_identity, generation_expression, column_default
        FROM information_schema.columns
        WHERE table_name = %s
          AND table_schema = %s
        ORDER BY ordinal_position;
    """, (table_name, table_schema))
    columns = cur.fetchall()

    value_column = ""
    editable_fields = []
    for col_name, data_type, is_identity, generation_expression, column_default in columns:
        if data_type == "boolean" and "active" in col_name.lower():
            active_column = col_name

        if not value_column and ("char" in data_type or "text" in data_type) \
           and col_name.lower() != pk_column.lower() and col_name.lower() != "tenantid":
            value_column = col_name

        if is_identity == "YES":
            continue
        if column_default and "nextval" in column_default:
            continue
        if generation_expression and generation_expression.strip():
            continue

        # Writable columns, keys included: a composite key is itself a writable
        # foreign key on a link table.
        editable_fields.append(col_name)

    column_names = [c[0] for c in columns]

    # A lookup is a label, not just an arbitrary text column. The first text
    # column is a reasonable fallback (Company -> Name, Invoice -> InvoiceNumber),
    # but it gets a formal person wrong: Contact would show "Harriet" instead of
    # "Jones, Harriet". The generator cannot infer a name order or a separator
    # from the schema, so LookupLabels declares it per app.
    #
    # With no text column at all the label is the key, which the dialect renders
    # as a cast to text.
    label = lookup_labels.get(table_name)
    if label:
        label_columns = label.get("columns", [])
        missing = [c for c in label_columns if c not in column_names]
        if not label_columns or missing:
            raise ValueError(
                f"LookupLabels['{table_name}'] declares no columns" if not label_columns
                else f"LookupLabels['{table_name}'] names columns that do not exist: {missing}"
            )
        label_separator = label.get("separator", " ")
    elif value_column:
        label_columns = [value_column]
        label_separator = " "
    else:
        label_columns = []
        label_separator = " "

    if label_columns:
        columns_literal = csharp_string_list(label_columns)
        label_literal = f'new LookupLabel({columns_literal}, "{csharp_string(label_separator)}")'
    else:
        label_literal = "LookupLabel.Key"

    lookup_filter = " ".join(lookup_filters.get(table_name, "").split()).rstrip(";")
    filter_literal = f'"{csharp_string(lookup_filter)}"' if lookup_filter else "null"

    active_literal = f'"{csharp_string(active_column)}"' if active_column else "null"

    class_code = []
    class_code.append(f"using {modelsns};")
    class_code.append(f"using {lookupns};")
    class_code.append("using _0nline.Shared.Db.Contract.Models;")
    class_code.append("using _0nline.Shared.Db.Service;")
    class_code.append("")
    class_code.append(f"/// This is auto-generated from QueryGenerator.py - do not modify.")
    class_code.append(f"namespace {ns}")
    class_code.append("{")
    class_code.append(f"    {scope} partial class {class_name}{base_class} : {base_class}<{class_name}>")
    class_code.append("    {")
    class_code.append(f"        public {class_name}{base_class}()")
    class_code.append(f'            : base(new DbTableDescriptor("{csharp_string(table_name)}")')
    class_code.append("            {")
    class_code.append(f"                KeyColumns = {csharp_string_list(pk_columns)},")
    class_code.append(f"                EditableColumns = {csharp_string_list(editable_fields)},")
    class_code.append(f"                ActiveColumn = {active_literal},")
    class_code.append(f"                LookupLabel = {label_literal},")
    class_code.append(f"                LookupFilter = {filter_literal}")
    class_code.append("            })")
    class_code.append("        { }")
    class_code.append("    }")
    class_code.append("}")

    out_path = os.path.join(output_dir, f"{class_name}{base_class}.cs")
    with open(out_path, "w") as f:
        f.write("\n".join(class_code))

print(f"Generation complete, output directory: {output_dir}")
cur.close()
conn.close()
