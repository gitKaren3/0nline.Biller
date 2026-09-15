import os
import re
import json

# --- Configuration ---
base_dir = os.path.dirname(os.path.abspath(__file__))
config_path = os.path.join(base_dir, "ControllerGenerator.json")

with open(config_path, "r", encoding="utf-8") as f:
    config = json.load(f)

target_namespace = config["Namespace"]
interface_folder_path = os.path.join(base_dir, config["InputDir"])
App = config["App"]
sub_route = App.lower()
output_dir = os.path.join(base_dir, "Generated")

os.makedirs(output_dir, exist_ok=True)

# --- Generate Base Controller ---
basecontrollercode = f"""using Microsoft.AspNetCore.Mvc;
using _0nline.Shared.Contract.Interfaces.DL;

/// This is auto-generated from a template - do not modify.
namespace {target_namespace}
{{
    [ApiController]
    [Route("api/{sub_route}/[controller]")]
    public abstract class {App}BaseController<T, TKey> : ControllerBase where T : class
    {{
        protected virtual IDLBaseService<T> DbService {{ get; }}

        public {App}BaseController(IDLBaseService<T> baseService) {{
            DbService = baseService;
        }}

        [HttpGet("{{id}}")]
        public virtual async Task<IActionResult> GetById(int id) {{
            var result = await DbService.GetOneAsync(id);
            return Ok(result);
        }}
    }}
}}
"""

with open(os.path.join(output_dir, f"{App}BaseController.cs"), "w", encoding="utf-8") as f:
    f.write(basecontrollercode)

print(f"Generated {App}BaseController")

# --- Generate Controllers from Interfaces ---
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

    entity_name = interface_name[1:].replace("Service", "")
    controller_name = entity_name + "Controller"

    is_readable = "IDLReadableService" in inheritance
    is_writable = "IDLWritableService" in inheritance

    sb = []
    sb.append("using Microsoft.AspNetCore.Mvc;")
    sb.append(f"using _0nline.{App}.DL.Contract.Interfaces;")
    sb.append(f"using _0nline.{App}.DL.Contract.Models.db;")
    sb.append("""
/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.""")
    sb.append(f"namespace {target_namespace}")
    sb.append("{")
    sb.append(f"    [Route(\"api/{sub_route}/[controller]\")]")
    sb.append(f"    public partial class {controller_name} : {App}BaseController<{entity_name}, int>")
    sb.append("    {")
    sb.append(f"        protected override {interface_name} DbService {{ get; }} ")
    sb.append("")
    sb.append(f"        public {controller_name}({interface_name} dbService) : base(dbService) {{")
    sb.append("             DbService = dbService;")
    sb.append("        }")

    if is_readable:
        sb.append("""
        [HttpGet]
        public virtual async Task<IActionResult> GetAll() {
            var result = await DbService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("lookup")]
        public virtual async Task<IActionResult> GetLookup() {
            var result = await DbService.GetLookupAsync();
            return Ok(result);
        }""")

    if is_writable:
        sb.append(f"""
        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] {entity_name} entity) {{
            var result = await DbService.CreateAsync(entity);
            return Ok(result);
        }}

        [HttpPut]
        public virtual async Task<IActionResult> Update([FromBody] {entity_name} entity) {{
            var result = await DbService.UpdateAsync(entity);
            return Ok(result);
        }}

        [HttpDelete("{{id}}")]
        public virtual async Task<IActionResult> DeleteOrDeactivate(int id) {{
            var result = await DbService.DeleteOrDeactivateAsync(id);
            return Ok(result);
        }}""")

    sb.append("    }")
    sb.append("}")

    output_file = os.path.join(output_dir, controller_name + ".cs")
    print(f"Generated {controller_name}")
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("\n".join(sb))

print("Controller generation complete.")

