
import os
import re
import json

# --- Configuration ---
base_dir = os.path.dirname(os.path.abspath(__file__))
config_path = os.path.join(base_dir, "HttpFileGenerator.json")

with open(config_path, "r", encoding="utf-8") as f:
    config = json.load(f)

App = config["App"]
BaseUrl = config["BaseUrl"]
sub_route = App.lower()
interface_folder_path = os.path.join(base_dir, config["InputDir"])
entity_folder_path = os.path.join(base_dir, config["EntityDir"])  # new config entry
output_dir = os.path.join(base_dir, "Generated")
os.makedirs(output_dir, exist_ok=True)

# --- Helper: generate sample JSON from entity class ---
def generate_sample_json(entity_name):
    # crude parse: look for property lines in the entity .cs file
    entity_file = os.path.join(entity_folder_path, f"{entity_name}.cs")
    if not os.path.exists(entity_file):
        return "{ \"id\": 0 }"

    props = []
    with open(entity_file, "r", encoding="utf-8") as f:
        for line in f:
            match = re.search(r"public\s+(\w+\??)\s+(\w+)\s*{", line)
            if match:
                cs_type, prop_name = match.groups()
                if cs_type.startswith("int") or cs_type == "long":
                    props.append(f'  "{prop_name}": 0')
                elif cs_type == "bool":
                    props.append(f'  "{prop_name}": false')
                elif cs_type == "DateTime":
                    props.append(f'  "{prop_name}": "2026-01-01T00:00:00"')
                else:
                    props.append(f'  "{prop_name}": "Sample"')
    return "{\n" + ",\n".join(props) + "\n}"

# --- Generate HTTP files from interfaces ---
for file in os.listdir(interface_folder_path):
    if not file.endswith(".cs"):
        continue

    with open(os.path.join(interface_folder_path, file), "r", encoding="utf-8") as f:
        content = f.read()

    match = re.search(r"public interface (I\w+Service)\s*:\s*([^{]+)", content)
    if not match:
        continue

    interface_name = match.group(1)
    inheritance = match.group(2)
    entity_name = interface_name[1:].replace("Service", "").strip()

    is_readable = "IDLReadableService" in inheritance
    is_writable = "IDLWritableService" in inheritance

    sb = []
    sb.append(f"### Get {entity_name} by ID")
    sb.append(f"GET {BaseUrl}/{sub_route}/{entity_name.lower()}/1\n")

    if is_readable:
        sb.append(f"### Get all {entity_name}s")
        sb.append(f"GET {BaseUrl}/{sub_route}/{entity_name.lower()}\n")
        sb.append(f"### Lookup {entity_name}s")
        sb.append(f"GET {BaseUrl}/{sub_route}/{entity_name.lower()}/lookup\n")

    if is_writable:
        sample_json = generate_sample_json(entity_name)
        sb.append(f"### Create {entity_name}")
        sb.append(f"POST {BaseUrl}/{sub_route}/{entity_name.lower()}")
        sb.append("Content-Type: application/json\n")
        sb.append(sample_json + "\n")

        sb.append(f"### Update {entity_name}")
        sb.append(f"PUT {BaseUrl}/{sub_route}/{entity_name.lower()}")
        sb.append("Content-Type: application/json\n")
        sb.append(sample_json + "\n")

        sb.append(f"### Delete {entity_name}")
        sb.append(f"DELETE {BaseUrl}/{sub_route}/{entity_name.lower()}/1\n")

    out_path = os.path.join(output_dir, f"{entity_name}.http")
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(sb))

print("HTTP file generation complete.")
