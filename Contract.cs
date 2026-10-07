using System.Net;
namespace RentalManager;
public static class Contract {
 public static string Render(Lease l,Property h,Tenant t,Landlord s){
 string E(object? v)=>WebUtility.HtmlEncode(v?.ToString()??"");
 var currency=System.Text.RegularExpressions.Regex.IsMatch(s.Currency??"",@"\A[A-Za-z]{3,10}\z")?(s.Currency ?? "TZS").ToUpperInvariant():"TZS";
 string Money(decimal v)=>$"{currency} {v:N2}";
 string Row(string key,object? v)=>$"<tr><th>{E(key)}</th><td>{E(v)}</td></tr>";
 return $"""
 <!doctype html><html lang="en"><head><meta charset="utf-8"><title>Rental agreement — lease {l.Id}</title><link rel="stylesheet" href="/contract.css"></head><body>
 <aside>Contract snapshot • Lease #{l.Id} • Generated {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC <button id="print">Print / Save PDF</button><p>Review the terms with the parties before signing. This is an editable agreement template.</p></aside>
 <article><div class="label">{E(h.Kind.ToUpperInvariant())} TENANCY</div><h1>Rental agreement</h1><p>Lease reference: RM-{l.Id:D6} · Currency: {currency}</p>
 <p>This agreement takes effect when both parties and their witnesses sign it, for the tenancy dates stated below. References to each party include their heirs, administrators and persons deriving rights through them.</p><h2>1. Parties</h2><table>{Row("Landlord",s.Name)}{Row("Landlord national ID",s.NationalId)}{Row("Landlord address",s.Address)}{Row("Landlord contact",s.Phone+" / "+s.Email)}{Row("Tenant",t.FullName)}{Row("Tenant national ID",t.NationalId)}{Row("Tenant address",t.Address)}{Row("Tenant contact",t.Phone+" / "+t.Email)}{Row("Emergency contact",t.EmergencyName+" / "+t.EmergencyPhone)}</table>
 <h2>2. Premises and duration</h2><p>The landlord declares lawful ownership of the premises, identified by building number {E(h.BuildingNumber)}. Both parties enter into this letting voluntarily.</p><p>The landlord lets <strong>{E(h.Name)}</strong>, at {E(h.Address)}, with <strong>{h.Bedrooms} bedroom(s)</strong> and {h.Bathrooms} bathroom(s), to the tenant for {E(h.Kind=="Shop"?"commercial shop use":"residential use")} by {l.Occupants} occupant(s).</p><p>Floor area: {h.FloorArea:N2} m². Permitted business/activity: {E(h.Kind=="Shop"?h.BusinessType:"Residential")}. Amenities: {E(h.Amenities)}.</p><p>The tenancy starts on <strong>{l.StartDate:dd MMMM yyyy}</strong> and ends on <strong>{l.EndDate:dd MMMM yyyy}</strong>. Lease status at generation: {E(l.Status)}.</p>
 <h2>3. Rent, deposit and payment account</h2><p>Monthly rent: <strong>{Money(l.MonthlyRent)}</strong>, The agreed rent instalment is <strong>{Money(l.MonthlyRent*l.PaymentIntervalMonths)}</strong> for <strong>{l.PaymentIntervalMonths} month(s)</strong>, payable every {l.PaymentIntervalMonths} month(s), beginning in the tenancy start month, by day {l.DueDay} (or the start date if later for the first payment). Monthly rent is the reporting equivalent of this instalment, not an additional charge. Security deposit: <strong>{Money(l.Deposit)}</strong>. Payments must include reference RM-{l.Id:D6}; issued payment receipts remain the evidence of payment.</p><table>{Row("Receiving bank",s.BankName)}{Row("Account holder",s.AccountName)}{Row("Receiving account number",s.AccountNumber)}{Row("Tenant bank",t.BankName)}{Row("Tenant account holder",t.AccountName)}{Row("Tenant account number",t.AccountNumber)}</table>
 <h2>4. Obligations and renewal</h2>
 <ol><li>The tenant must use the premises only for {E(h.Kind=="Shop"?"lawful business consistent with the permitted activity":"lawful residential occupation")}.</li>
 <li>The tenant must not sublet without the landlord's consent.</li>
 <li>The tenant must not cause a nuisance to the landlord or neighbours.</li>
 <li>The tenant must keep the premises clean and satisfactory and carry out minor repairs.</li>
 <li>The tenant must pay the utility and service charges they incur.</li>
 <li>The tenant must consult the landlord before making internal or external alterations.</li>
 <li>The tenant must give {l.NoticeDays} days' notice before expiry if they do not intend to continue.</li>
 <li>While the tenant complies with this agreement, the landlord must allow peaceful use without disturbance from the landlord or anyone claiming through them.</li></ol>
 <p>The agreement may be renewed by mutual agreement every {l.RenewalIntervalMonths} month(s) as needed. Renewal is not automatic; record agreed dates in a renewed lease. Rent may change according to market conditions only as agreed by both parties.</p>
 <h3>Additional landlord terms</h3><p class="terms">{E(s.ContractTerms)}</p><p>Notice period agreed by the parties: {l.NoticeDays} days, subject to applicable law.</p>
 <h2>5. Special terms</h2><p class="terms">{E(string.IsNullOrWhiteSpace(l.SpecialTerms)?"No additional terms recorded.":l.SpecialTerms)}</p>
 <h2>6. Acceptance and signatures</h2><p>The parties confirm that they have read this agreement and agree to the terms above. Any change must be recorded in writing and accepted by both parties.</p><div class="signatures"><div><strong>Landlord: {E(s.Name)}</strong><p>Signature: ____________________</p><p>Date: ________________________</p></div><div><strong>Tenant: {E(t.FullName)}</strong><p>Signature: ____________________</p><p>Date: ________________________</p></div></div><div class="signatures"><div><strong>Landlord's witness: {E(l.LandlordWitnessName)}</strong><p>Title: {E(l.LandlordWitnessTitle)} · Phone: {E(l.LandlordWitnessPhone)}</p><p>Signature: ____________________</p><p>Date: ____________________</p></div><div><strong>Tenant's witness: {E(l.WitnessName)}</strong><p>Title: {E(l.TenantWitnessTitle)} · Phone: {E(l.WitnessPhone)}</p><p>Signature: ____________________</p><p>Date: ____________________</p></div></div>
 </article><script src="/contract.js"></script></body></html>
 """;
 }
}
