window.rental = {
  print: () => window.print(),
  download: (name, content, mime) => {
    const url=URL.createObjectURL(new Blob([content],{type:mime}));
    const a=document.createElement('a');a.href=url;a.download=name;a.click();
    setTimeout(()=>URL.revokeObjectURL(url),1000);
  }
};

// Build a standalone report from rendered, escaped DOM text. Save as PDF in the print dialog.
window.rental.reportPdf = (target, title) => {
  const source = target ? document.getElementById(target) : document.querySelector('main');
  if (!source) throw new Error('Report section was not found.');
  const popup = window.open('', '_blank');
  if (!popup) throw new Error('Allow pop-ups to export a PDF report.');
  const doc = popup.document;
  doc.title = title;
  const style = doc.createElement('style');
  style.textContent = '@page{size:A4 landscape;margin:12mm}body{font:12px Arial,sans-serif;color:#111}h1{font-size:22px}table{border-collapse:collapse;width:100%;margin:16px 0}th,td{border:1px solid #aaa;padding:6px;text-align:left;overflow-wrap:anywhere}thead{display:table-header-group}tr{break-inside:avoid}button,input,select,.toolbar,.reportFilters,.editor,.deleteConfirm,.pdfReport,footer{display:none}a{color:#111;text-decoration:none}.tableWrap{overflow:visible}';
  doc.head.appendChild(style);
  const heading = doc.createElement('h1');heading.textContent = title;doc.body.appendChild(heading);
  const note=doc.createElement('p');note.textContent='Chwaya Rental Manager · Generated '+new Date().toLocaleString()+' · Choose Save as PDF in the print dialog.';doc.body.appendChild(note);
  const copy = source.cloneNode(true);
  copy.querySelectorAll('script,button,input,select,textarea,.toolbar,.reportFilters,.editor,.deleteConfirm,.pdfReport,footer,.error,.notice').forEach(e=>e.remove());
  copy.querySelectorAll('table').forEach(table=>{
    const actionIndex=Array.from(table.querySelectorAll('thead th')).findIndex(th=>th.textContent.trim()==='Actions');
    if(actionIndex>=0)table.querySelectorAll('tr').forEach(row=>{if(row.children[actionIndex])row.children[actionIndex].remove();});
  });
  doc.body.appendChild(copy);
  popup.focus();popup.print();
};
