"""Architecture and source checks; build and database tests are separate checks."""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
for p in root.rglob('*.csproj'):
 if 'obj' not in p.parts: ET.parse(p)
domain=root/'src/RentalManager.Domain'
application=root/'src/RentalManager.Application'
for directory in (domain,application):
 for p in directory.glob('*.cs'):
  assert 'using Microsoft.EntityFrameworkCore' not in p.read_text(),p
  assert 'using Microsoft.AspNetCore' not in p.read_text(),p
for p in (root/'Components').rglob('*.razor'):
 assert '.Result' not in p.read_text(),p
for p in (root/'Pages').rglob('*.cs'):
 assert 'RentalDbContext' not in p.read_text(),p
migrations=root/'src/RentalManager.Infrastructure/Migrations'
assert (migrations/'RentalDbContextModelSnapshot.cs').exists()
assert len(list(migrations.glob('*.cs')))>=4
assert 'AddScoped<IRentalRepository,EfRentalRepository>' in (root/'Program.cs').read_text()
assert 'AddScoped<ICurrentUser,BlazorCurrentUser>' in (root/'Program.cs').read_text()
assert '<Compile Remove="Tests/**/*.cs;src/**/*.cs"' in (root/'RentalManager.csproj').read_text()
print('PASS: project XML, layer isolation, migration locations, UI async rendering and dependency registration')
