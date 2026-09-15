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
connection_string = config["ConnectionString"]
scope = config["Scope"]
Depluralize = config.get("Depluralize", "false").lower() == "true"
base_class = config.get("BaseClass", "{base_class}")
quote_identifier_format = config.get("QuoteIdentifierFormat", "\"{0}\"")  # default Postgres

def quote_identifier(name: str) -> str:
    """Apply configured identifier quoting format."""
    return quote_identifier_format.format(name)

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

for table_name, table_schema in tables:
    class_name = get_class_name(table_name)
    is_tenant_aware = False
    active_Column = ""
    tenant_column_name = "TenantID"

    # Primary key
    cur.execute("""
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
          ON tc.constraint_name = kcu.constraint_name
         AND tc.table_schema = kcu.table_schema
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_name = %s
          AND tc.table_schema = %s
        LIMIT 1;
    """, (table_name, table_schema))
    pk_row = cur.fetchone()
    pk_column = pk_row[0] if pk_row else ""

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
        if col_name.lower() == "tenantid":
            is_tenant_aware = True
            tenant_column_name = col_name

        if data_type == "boolean" and "active" in col_name.lower():
            active_Column = col_name
        if not value_column and ("char" in data_type or "text" in data_type) \
           and col_name.lower() != pk_column.lower() and col_name.lower() != "tenantid":
            value_column = col_name
        if col_name.lower() == pk_column.lower():
            continue

        if is_identity == "YES":
            continue
        if generation_expression and generation_expression.strip():
            continue

        editable_fields.append(f"\"{col_name}\"")

    if not value_column:
        value_column = "''"

    fields_array = f"new string[] {{ {', '.join(editable_fields)} }}"

    # Build class code
    class_code = []
    class_code.append(f"using {modelsns};")
    class_code.append("using _0nline.Shared.Db.Service;")
    class_code.append("")
    class_code.append(f"/// This is auto-generated from QueryGenerator.py - do not modify.")
    class_code.append(f"namespace {ns}")
    class_code.append("{")
    class_code.append(f"    {scope} partial class {class_name}{base_class} : {base_class}<{class_name}>")
    class_code.append("    {")
    class_code.append(f"        public {class_name}{base_class}()")
    class_code.append(f"            : base(\"{table_name}\", {fields_array}, \"{pk_column}\")")
    class_code.append(f"        {{ }}")

    if is_tenant_aware:
        class_code.append(f"        public override string AllQuery => AddFilter(base.AllQuery, \"{quote_identifier(tenant_column_name)} = @TenantId\");")
        if pk_column:
            class_code.append(f"        public override string ByIdQuery => AddFilter(base.ByIdQuery, \"{quote_identifier(tenant_column_name)} = @TenantId\");")
        class_code.append(f"        public override string LookupQuery => AddFilter($\"SELECT {quote_identifier(pk_column)}, {quote_identifier(value_column)} FROM {quote_identifier(table_name)}\", \"{quote_identifier(tenant_column_name)} = @TenantId\");")
        class_code.append(f"        public override string UpdateQuery => AddFilter(base.UpdateQuery, \"{quote_identifier(tenant_column_name)} = @TenantId\");")
        class_code.append(f"        public override string DeleteQuery => AddFilter(base.DeleteQuery, \"{quote_identifier(tenant_column_name)} = @TenantId\");")
    else:
        class_code.append("        public override string AllQuery => base.AllQuery;")
        if pk_column:
            class_code.append("        public override string ByIdQuery => base.ByIdQuery;")
        class_code.append(f"        public override string LookupQuery => $\"SELECT {quote_identifier(pk_column)} AS Key, {quote_identifier(value_column)} AS Value FROM {quote_identifier(table_name)}\";")
        class_code.append("        public override string UpdateQuery => base.UpdateQuery;")
        class_code.append("        public override string DeleteQuery => base.DeleteQuery;")

    if active_Column:
        class_code.append(f'        public override string DeactivateQuery => $"UPDATE {quote_identifier(table_name)} SET {quote_identifier(active_Column)} = FALSE WHERE {quote_identifier(pk_column)} = @Id RETURNING *";')
    else:
        class_code.append("        public override string DeactivateQuery => string.Empty;")

    class_code.append("    }")
    class_code.append("}")

    out_path = os.path.join(output_dir, f"{class_name}{base_class}.cs")
    with open(out_path, "w") as f:
        f.write("\n".join(class_code))

print(f"Generation complete, output directory: {output_dir}")
cur.close()
conn.close()
