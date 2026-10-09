const assert=require('node:assert/strict');
const {chromium}=require('playwright');
(async()=>{
 const browser=await chromium.launch({headless:true});
 const admin=await browser.newPage({viewport:{width:1440,height:1000}});
 const base=process.env.RENTAL_UI_URL||'http://127.0.0.1:5080';
 const go=path=>admin.goto(base+path,{waitUntil:'networkidle'});
 const save=async message=>{await admin.locator('.editor').getByRole('button',{name:'Save changes',exact:true}).click();await admin.getByText(message,{exact:true}).waitFor();};
 try{
  await go('/Account/Login');await admin.locator('#Username').fill('browser-admin');await admin.locator('#Password').fill('BrowserTest_2026!Only');await Promise.all([admin.waitForURL(base+'/'),admin.getByRole('button',{name:'Sign in',exact:true}).click()]);
  await go('/settings');let form=admin.locator('.editor');await form.getByLabel(/^Name/i).fill('Test landlord');await form.getByLabel(/^Address/i).fill('Test area');await form.getByLabel(/^Phone/i).fill('000000');await form.getByLabel(/^Bank Name/i).fill('Test bank');await form.getByLabel(/^Account Name/i).fill('Test account');await form.getByLabel(/^Account Number/i).fill('0000123');await save('Settings saved.');
  const start=new Date(Date.now()+3*60*60*1000).toISOString().slice(0,10);const end=new Date(Date.now()+3*60*60*1000+14*86400000).toISOString().slice(0,10);
  const fixture=async(name,index)=>{
   await go('/tenants');await admin.getByRole('button',{name:'+ Add tenant',exact:true}).click();form=admin.locator('.editor');await form.getByLabel(/^Full Name/i).fill(name);await form.getByLabel(/^National Id/i).fill('PORTAL-'+index);await form.getByLabel(/^Phone/i).fill('00000'+index);await form.getByLabel(/^Address/i).fill('Test area');await save('Record saved.');
   const property='House for '+name;await go('/houses');await admin.getByRole('button',{name:'+ Add house',exact:true}).click();form=admin.locator('.editor');await form.getByLabel(/^Name/i).fill(property);await form.getByLabel(/^Address/i).fill('Test area');await form.getByLabel(/^Advertised monthly rent/i).fill('650000');await save('Record saved.');
   await go('/leases');await admin.getByRole('button',{name:'+ Add lease',exact:true}).click();form=admin.locator('.editor');await form.getByLabel(/^House \/ shop/i).selectOption({label:'House · '+property});await form.getByRole('combobox',{name:/^Tenant/i}).selectOption({label:name});await form.getByLabel(/^Start Date/i).fill(start);await form.getByLabel(/^End Date/i).fill(end);await save('Record saved.');
   await admin.locator('tbody tr').filter({hasText:property}).getByRole('button',{name:'Generate contract',exact:true}).click();await admin.getByText(/Contract #\d+ saved/).waitFor();
   await go('/contracts');return await admin.locator('a[href^="/documents/contracts/"]').first().getAttribute('href');
  };
  const ownDocument=await fixture('Portal One',1);const otherDocument=await fixture('Portal Two',2);
  await go('/clients');await admin.getByLabel('Tenant',{exact:true}).selectOption({label:'Portal One'});await admin.getByLabel('Client username',{exact:true}).fill('portal-one');await admin.getByLabel('Temporary password',{exact:true}).fill('PortalInitial_2026!');await admin.getByRole('button',{name:'Create client account',exact:true}).click();await admin.getByText(/Client account created\./).waitFor();
  const client=await browser.newPage({viewport:{width:1200,height:900}});const clientGo=path=>client.goto(base+path,{waitUntil:'networkidle'});
  const login=async password=>{await clientGo('/Client/Login');await client.locator('#Username').fill('portal-one');await client.locator('#Password').fill(password);await Promise.all([client.waitForURL(/\/(Client\/Password|client)$/),client.getByRole('button',{name:'Sign in',exact:true}).click()]);};
  await login('PortalInitial_2026!');assert(client.url().endsWith('/Client/Password'));
  await clientGo('/client');assert(client.url().endsWith('/Client/Password'),'Temporary-password users cannot open the portal');
  await client.locator('#CurrentPassword').fill('PortalInitial_2026!');await client.locator('#NewPassword').fill('PortalChanged_2026!');await client.locator('#ConfirmPassword').fill('PortalChanged_2026!');await Promise.all([client.waitForURL('**/Client/Login'),client.getByRole('button',{name:'Save and sign in again',exact:true}).click()]);
  await login('PortalChanged_2026!');await clientGo('/client');await client.getByRole('heading',{name:'House for Portal One',exact:true}).waitFor();assert.equal(await client.getByText('House for Portal Two',{exact:true}).count(),0);
  await client.evaluate(()=>window.clientNavigationMarker='same-document');
  const clientNav=client.getByRole('navigation',{name:'Client navigation'});
  await clientNav.getByRole('link',{name:'Contracts',exact:true}).click();await client.getByRole('heading',{name:'My contracts',exact:true}).waitFor();
  assert.equal(await client.evaluate(()=>window.clientNavigationMarker),'same-document','Contracts navigation must not reload the document');
  await clientNav.getByRole('link',{name:'Notifications',exact:true}).click();await client.getByRole('heading',{name:'My notifications',exact:true}).waitFor();
  await clientNav.getByRole('link',{name:'My rental',exact:true}).click();await client.getByRole('heading',{name:'My rental',exact:true}).waitFor();
  await client.getByRole('link',{name:'View contract',exact:true}).click();await client.getByRole('heading',{name:'My contracts',exact:true}).waitFor();
  await client.goBack();await client.getByRole('heading',{name:'My rental',exact:true}).waitFor();
  await client.goForward();await client.getByRole('heading',{name:'My contracts',exact:true}).waitFor();
  assert.equal(await client.evaluate(()=>window.clientNavigationMarker),'same-document','Client navigation and history must update without refresh');
  const ownLink=client.locator('a[href^="/client/documents/contracts/"]');assert.equal(await ownLink.count(),1);assert.equal((await client.request.get(base+'/client'+ownDocument)).status(),200);assert.equal((await client.request.get(base+'/client'+otherDocument)).status(),404);
  const adminDocument=await client.request.get(base+ownDocument,{maxRedirects:0});assert.equal(adminDocument.status(),302);assert((adminDocument.headers().location||'').includes('/Account/Denied'));
  await clientGo('/settings');assert.equal(await client.getByRole('heading',{name:'Settings',exact:true}).count(),0);
  await clientGo('/client/notifications');await client.getByText(/Your rental term ends on/).waitFor();
  await clientGo('/client');await client.getByRole('button',{name:'Share rental decision',exact:true}).click();await client.getByLabel('Requested additional months',{exact:true}).fill('6');await client.getByLabel('Message to landlord',{exact:true}).fill('Please renew my rental term.');await client.getByRole('button',{name:'Send decision to landlord',exact:true}).click();await client.getByText('Your rental decision was sent to the landlord.',{exact:true}).waitFor();
  await go('/clients?tab=renewals');await admin.getByText('Please renew my rental term.',{exact:true}).waitFor();await admin.getByLabel('Response to client',{exact:true}).fill('Please contact me to agree the new term.');await admin.getByRole('button',{name:'Send response and mark reviewed',exact:true}).click();await admin.getByText(/Response sent to the client portal/).waitFor();
  await clientNav.getByRole('link',{name:'Notifications',exact:true}).click();await client.getByText('Please contact me to agree the new term.',{exact:true}).waitFor();
  await client.setViewportSize({width:390,height:844});await clientGo('/client');assert(await client.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await client.screenshot({path:'/tmp/rental-comments-client.png',fullPage:true});
  console.log('PASS: client provisioning, forced password change, own-only contract access, administrator isolation, expiry reminders, renewal submission/response automatic client navigation/history and mobile client layout.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
