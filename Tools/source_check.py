"""Static consistency checks; not a substitute for dotnet build or SQL integration tests."""
import json,re,xml.etree.ElementTree as ET
from pathlib import Path
root=Path(__file__).resolve().parents[1]
config=json.loads((root/'appsettings.json').read_text())
assert config['Database']['AutoMigrate'] is True
assert config['Database']['ConnectionName']=='DefaultConnectionOnHrPayMisHubDbDev'
for connection in config['ConnectionStrings'].values():
 assert 'Database=Chwaya_rentalDb;' in connection
 assert 'PayrollHubDb' not in connection
ET.parse(root/'RentalManager.csproj')
ET.parse(root/'Tests/RentalManager.Tests.csproj')
program=(root/'Program.cs').read_text()
assert 'AddInteractiveServerComponents' in program and 'AddInteractiveServerRenderMode' in program
assert 'WebAssembly' not in program and 'UseSqlServer' in program and 'MigrateAsync' in program
assert 'EnsureCreated' not in program
migration=(root/'Migrations/20261005130000_InitialRentalSqlServer.cs').read_text()
snapshot=(root/'Migrations/RentalDbContextModelSnapshot.cs').read_text()
manifest=json.loads((root/'Database/model-manifest.json').read_text())
for name,fields in manifest.items():
 assert f'modelBuilder.Entity("RentalManager.{name}"' in snapshot
 for field in fields:
  assert '"'+field['name']+'"' in snapshot
  assert re.search(r'\b'+field['name']+r'=table.Column',migration)
assert 'TR_Leases_NoOverlap' in migration and 'UseSqlOutputClause(false)' in snapshot
assert 'PropertyId' in (root/'Services/RentalService.cs').read_text()
assert 'commercial shop use' in (root/'Contract.cs').read_text()
print('PASS: project XML, connection selection, server rendering, migration/snapshot field manifest, shop contract configuration')
