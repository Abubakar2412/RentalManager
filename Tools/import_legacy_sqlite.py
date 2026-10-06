"""Export the previous package's SQLite data as a SQL Server import script.
Creates a file only; does not connect to or modify SQL Server."""
import argparse,sqlite3,json,uuid
from pathlib import Path
from decimal import Decimal
def literal(value):
 if value is None:return 'NULL'
 if isinstance(value,bool):return '1' if value else '0'
 if isinstance(value,(int,float,Decimal)):return str(value)
 return "N'"+str(value).replace("'","''")+"'"
def generate(source):
 con=sqlite3.connect(f'file:{Path(source).resolve().as_posix()}?mode=ro',uri=True)
 manifest=json.loads((Path(__file__).resolve().parents[1]/'Database/model-manifest.json').read_text())
 tables={'Property':'Properties','Tenant':'Tenants','Lease':'Leases','Payment':'Payments','Landlord':'Landlords','Admin':'Admins','ContractSnapshot':'Contracts','AuditEntry':'Audit'}
 rows={k:[] for k in tables}
 for cls,old in [('Property','Houses'),('Tenant','Tenants'),('Lease','Leases'),('Payment','Payments'),('Landlord','Settings')]:
  for id,payload in con.execute(f'SELECT Id,Data FROM {old} ORDER BY Id'):
   record=json.loads(payload,parse_float=Decimal);record['id']=id
   if cls=='Property':record.update(kind='House',floorArea=0,businessType='')
   if cls=='Lease':record['propertyId']=record.pop('houseId')
   if cls=='Tenant':record['nationalId']=record['nationalId'].strip().upper()
   rows[cls].append(record)
 for id,name,hash in con.execute('SELECT Id,Username,Hash FROM Admin'):
  rows['Admin'].append(dict(id=id,username=name,passwordHash=hash,securityStamp=uuid.uuid4().hex))
 for id,lease,at,html in con.execute('SELECT Id,LeaseId,CreatedAt,Html FROM Contracts'):
  rows['ContractSnapshot'].append(dict(id=id,leaseId=lease,createdAt=at,html=html))
 for id,at,actor,action,entity,entity_id in con.execute('SELECT Id,At,Actor,Action,Entity,EntityId FROM Audit'):
  rows['AuditEntry'].append(dict(id=id,at=at,actor=actor,action=action,entity=entity,entityId=entity_id))
 con.close()
 sql=['-- Generated import. Contains confidential rental records; keep private.',
      '-- First run the new app to apply EF migrations, then stop it before importing.',
      'USE [Chwaya_rentalDb];','SET XACT_ABORT ON;','BEGIN TRY',' BEGIN TRANSACTION;']
 for table in tables.values():
  sql.append(f" IF EXISTS(SELECT 1 FROM [{table}]) THROW 51002, 'Import requires empty rental tables.', 1;")
 for cls,table in tables.items():
  fields=[f for f in manifest[cls] if f['name']!='RowVersion']
  identity=any(f['identity'] for f in fields)
  if rows[cls] and identity:sql.append(f' SET IDENTITY_INSERT [{table}] ON;')
  for record in rows[cls]:
   values=[]
   for field in fields:
    key=field['name'][0].lower()+field['name'][1:]
    default=None if field['nullable'] else '' if field['type']=='string' else 0
    values.append(literal(record.get(key,default)))
   sql.append(f' INSERT INTO [{table}] ('+','.join('['+f['name']+']' for f in fields)+') VALUES ('+','.join(values)+');')
  if rows[cls] and identity:sql.append(f' SET IDENTITY_INSERT [{table}] OFF;')
 sql+=[' COMMIT TRANSACTION;','END TRY','BEGIN CATCH',' IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;',' THROW;','END CATCH;']
 return '\n'.join(sql)+'\n'
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('sqlite_file');parser.add_argument('output_sql');args=parser.parse_args()
 Path(args.output_sql).write_text(generate(args.sqlite_file),encoding='utf-8')
 print('Import script created. No SQL Server changes were made.')
