using System.Net;
using PawCare.Api.Models;

namespace PawCare.Api.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private const string HospitalName = "PawCare Veterinary Hospital";
        private const string HospitalPhone = "(555) 019-CARE";
        private const string HospitalEmail = "care@pawcare.local";
        private const string HospitalAddress = "104 Blossom Ridge Road, Suite 200, Metro City";

        public EmailTemplateResult BuildTemplate(string notificationType, Patient patient)
        {
            var ownerName = WebUtility.HtmlEncode(patient.Owner?.Name ?? "Valued Pet Parent");
            var patientName = WebUtility.HtmlEncode(patient.Name);
            var species = WebUtility.HtmlEncode(patient.Species);
            var breed = WebUtility.HtmlEncode(patient.Breed);
            var wardName = WebUtility.HtmlEncode(patient.Ward?.Name ?? "Main Inpatient Ward");
            var clinicalStatus = WebUtility.HtmlEncode(patient.ClinicalStatus);
            var admissionStatus = WebUtility.HtmlEncode(patient.AdmissionStatus);
            var normalizedType = notificationType.Trim().ToUpperInvariant();

            return normalizedType switch
            {
                "DECEASED" => BuildDeceasedTemplate(ownerName, patientName, species, breed),
                "DISCHARGED" or "TREATED" or "TREATED/DISCHARGED" => BuildDischargedTemplate(ownerName, patientName, species, breed, wardName),
                _ => BuildTreatmentTemplate(normalizedType, ownerName, patientName, species, breed, wardName, clinicalStatus, admissionStatus)
            };
        }

        private EmailTemplateResult BuildDeceasedTemplate(string ownerName, string patientName, string species, string breed)
        {
            var subject = $"PawCare Memorial Notice: Heartfelt Condolences Regarding {patientName}";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{subject}</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; margin: 0; padding: 24px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
    .header {{ background: #0f172a; color: #ffffff; padding: 28px 24px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 20px; font-weight: 600; letter-spacing: -0.025em; }}
    .header p {{ margin: 6px 0 0; color: #94a3b8; font-size: 13px; }}
    .content {{ padding: 32px 28px; }}
    .condolence-box {{ background: #fef2f2; border-left: 4px solid #ef4444; padding: 16px 20px; border-radius: 6px; margin: 20px 0; }}
    .condolence-box p {{ margin: 0; color: #991b1b; font-size: 14px; font-weight: 500; }}
    .details-table {{ width: 100%; border-collapse: collapse; margin: 24px 0; font-size: 14px; }}
    .details-table td {{ padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }}
    .details-table td:first-child {{ color: #64748b; font-weight: 500; width: 35%; }}
    .details-table td:last-child {{ color: #0f172a; font-weight: 600; }}
    .footer {{ background: #f8fafc; padding: 20px 28px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; text-align: center; }}
    .footer strong {{ color: #334155; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <h1>{HospitalName}</h1>
      <p>Compassionate Veterinary Care & Support</p>
    </div>
    <div class=""content"">
      <p>Dear {ownerName},</p>
      
      <div class=""condolence-box"">
        <p>It is with heavy hearts and profound sorrow that our entire medical staff extends our deepest sympathies regarding <strong>{patientName}</strong>.</p>
      </div>

      <p>Our veterinary doctors and care team did everything possible to bring comfort, relief, and dignity to {patientName} during their stay with us. We understand how deeply cherished {patientName} was, and words cannot express how sorry we are for your loss.</p>

      <table class=""details-table"">
        <tr><td>Patient Name</td><td>{patientName}</td></tr>
        <tr><td>Species / Breed</td><td>{species} ({breed})</td></tr>
        <tr><td>Recorded Status</td><td>Deceased</td></tr>
      </table>

      <p>Our bereavement counseling services and support team are available whenever you feel ready. Please do not hesitate to contact our care coordinator if you require assistance with aftercare arrangements or memorial keepsakes.</p>

      <p style=""margin-top: 28px;"">With our deepest condolences and heartfelt sympathy,<br>
      <strong>The Staff & Doctors at {HospitalName}</strong></p>
    </div>
    <div class=""footer"">
      <p><strong>{HospitalName}</strong> &bull; {HospitalAddress}<br>
      Phone: {HospitalPhone} &bull; Email: {HospitalEmail}</p>
    </div>
  </div>
</body>
</html>";

            var text = $@"DEAR {ownerName},

It is with heavy hearts and profound sorrow that our entire medical staff at {HospitalName} extends our deepest sympathies regarding {patientName} ({species}, {breed}).

Our veterinary doctors and care team did everything possible to bring comfort and relief to {patientName} during their stay with us. We understand how deeply cherished {patientName} was, and words cannot express how sorry we are for your loss.

PATIENT SUMMARY:
- Patient: {patientName}
- Species/Breed: {species} ({breed})
- Recorded Status: DECEASED

Our staff and bereavement coordinators are available whenever you need us. Please reach out to us at {HospitalPhone} or {HospitalEmail} if you need assistance with aftercare arrangements.

With deepest sympathy,
The Staff & Doctors at {HospitalName}
{HospitalAddress}";

            return new EmailTemplateResult(subject, html.Trim(), text.Trim());
        }

        private EmailTemplateResult BuildDischargedTemplate(string ownerName, string patientName, string species, string breed, string wardName)
        {
            var subject = $"PawCare Update: Wonderful News! {patientName} is Discharged";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{subject}</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; margin: 0; padding: 24px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
    .header {{ background: #047857; color: #ffffff; padding: 28px 24px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 20px; font-weight: 600; letter-spacing: -0.025em; }}
    .header p {{ margin: 6px 0 0; color: #a7f3d0; font-size: 13px; }}
    .content {{ padding: 32px 28px; }}
    .badge-box {{ background: #ecfdf5; border-left: 4px solid #10b981; padding: 16px 20px; border-radius: 6px; margin: 20px 0; }}
    .badge-box p {{ margin: 0; color: #065f46; font-size: 14px; font-weight: 500; }}
    .details-table {{ width: 100%; border-collapse: collapse; margin: 24px 0; font-size: 14px; }}
    .details-table td {{ padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }}
    .details-table td:first-child {{ color: #64748b; font-weight: 500; width: 35%; }}
    .details-table td:last-child {{ color: #0f172a; font-weight: 600; }}
    .checklist {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px 20px; margin: 20px 0; }}
    .checklist h4 {{ margin: 0 0 10px; font-size: 13px; color: #0f172a; text-transform: uppercase; letter-spacing: 0.05em; }}
    .checklist ul {{ margin: 0; padding-left: 18px; color: #475569; font-size: 13px; }}
    .checklist li {{ margin-bottom: 6px; }}
    .footer {{ background: #f8fafc; padding: 20px 28px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; text-align: center; }}
    .footer strong {{ color: #334155; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <h1>{HospitalName}</h1>
      <p>Patient Discharge Notice & Instructions</p>
    </div>
    <div class=""content"">
      <p>Dear {ownerName},</p>
      
      <div class=""badge-box"">
        <p>Great news! <strong>{patientName}</strong> has recovered sufficiently and has been officially approved for <strong>DISCHARGE</strong>!</p>
      </div>

      <p>Our attending veterinarians have completed final evaluations in <strong>{wardName}</strong>. {patientName} is ready to return home to your loving care.</p>

      <table class=""details-table"">
        <tr><td>Patient Name</td><td>{patientName}</td></tr>
        <tr><td>Species / Breed</td><td>{species} ({breed})</td></tr>
        <tr><td>Discharged From</td><td>{wardName}</td></tr>
        <tr><td>Current Status</td><td>DISCHARGED / RECOVERED</td></tr>
      </table>

      <div class=""checklist"">
        <h4>Discharge & Pick-Up Checklist:</h4>
        <ul>
          <li>Pick-up hours are daily from 8:00 AM to 7:00 PM at our main reception.</li>
          <li>Please bring a secure crate, carrier, or leash for safe transit home.</li>
          <li>Our pharmacy team will provide any remaining prescription medications and dietary advice.</li>
          <li>If you observe any unusual lethargy or symptoms post-discharge, contact our 24/7 hotline immediately.</li>
        </ul>
      </div>

      <p>Thank you for trusting {HospitalName} with {patientName}'s healthcare.</p>

      <p style=""margin-top: 28px;"">Warm regards,<br>
      <strong>The Medical Care Team at {HospitalName}</strong></p>
    </div>
    <div class=""footer"">
      <p><strong>{HospitalName}</strong> &bull; {HospitalAddress}<br>
      Phone: {HospitalPhone} &bull; Emergency Line: {HospitalPhone} &bull; Email: {HospitalEmail}</p>
    </div>
  </div>
</body>
</html>";

            var text = $@"DEAR {ownerName},

Great news! {patientName} ({species}, {breed}) has recovered sufficiently and has been officially marked as DISCHARGED from {HospitalName}!

PATIENT SUMMARY:
- Patient: {patientName}
- Species/Breed: {species} ({breed})
- Ward: {wardName}
- Status: DISCHARGED / RECOVERED

PICK-UP INSTRUCTIONS:
1. Pick-up hours are daily from 8:00 AM to 7:00 PM at our main reception.
2. Please bring a secure carrier or leash for transport.
3. Our staff will hand over prescription medications, dosage schedules, and home recovery instructions.
4. For urgent questions after returning home, our 24/7 hotline is available at {HospitalPhone}.

Thank you for trusting {HospitalName} with {patientName}'s medical care.

Warm regards,
The Medical Care Team at {HospitalName}
{HospitalAddress}";

            return new EmailTemplateResult(subject, html.Trim(), text.Trim());
        }

        private EmailTemplateResult BuildTreatmentTemplate(string type, string ownerName, string patientName, string species, string breed, string wardName, string clinicalStatus, string admissionStatus)
        {
            var statusDisplay = clinicalStatus.Replace("_", " ");
            var subject = $"PawCare Medical Update: {patientName} Status — {statusDisplay}";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{subject}</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; margin: 0; padding: 24px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
    .header {{ background: #0284c7; color: #ffffff; padding: 28px 24px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 20px; font-weight: 600; letter-spacing: -0.025em; }}
    .header p {{ margin: 6px 0 0; color: #bae6fd; font-size: 13px; }}
    .content {{ padding: 32px 28px; }}
    .status-box {{ background: #f0f9ff; border-left: 4px solid #0284c7; padding: 16px 20px; border-radius: 6px; margin: 20px 0; }}
    .status-box p {{ margin: 0; color: #0369a1; font-size: 14px; font-weight: 500; }}
    .details-table {{ width: 100%; border-collapse: collapse; margin: 24px 0; font-size: 14px; }}
    .details-table td {{ padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }}
    .details-table td:first-child {{ color: #64748b; font-weight: 500; width: 35%; }}
    .details-table td:last-child {{ color: #0f172a; font-weight: 600; }}
    .note-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin: 20px 0; font-size: 13px; color: #475569; }}
    .footer {{ background: #f8fafc; padding: 20px 28px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; text-align: center; }}
    .footer strong {{ color: #334155; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <h1>{HospitalName}</h1>
      <p>Clinical Status & Daily Care Update</p>
    </div>
    <div class=""content"">
      <p>Dear {ownerName},</p>
      
      <div class=""status-box"">
        <p>This is a clinical status update regarding your pet <strong>{patientName}</strong>. The latest status is recorded as <strong>{statusDisplay}</strong>.</p>
      </div>

      <p>Our veterinary team in <strong>{wardName}</strong> is continuing attentive care, ongoing vitals checks, and administering targeted treatments per protocol.</p>

      <table class=""details-table"">
        <tr><td>Patient Name</td><td>{patientName}</td></tr>
        <tr><td>Species / Breed</td><td>{species} ({breed})</td></tr>
        <tr><td>Assigned Ward</td><td>{wardName}</td></tr>
        <tr><td>Admission Status</td><td>{admissionStatus}</td></tr>
        <tr><td>Clinical Status</td><td><span style=""display: inline-block; padding: 3px 10px; background: #e0f2fe; color: #0369a1; border-radius: 9999px; font-size: 12px; font-weight: 700;"">{statusDisplay}</span></td></tr>
      </table>

      <div class=""note-card"">
        <strong>Care Team Note:</strong><br>
        Daily rounds and vital signs assessments are continually tracked in {patientName}'s official medical chart. If you would like an in-depth conversation with the attending vet on duty, please call during hospital visitation hours.
      </div>

      <p style=""margin-top: 28px;"">Sincerely,<br>
      <strong>The Attending Clinical Staff at {HospitalName}</strong></p>
    </div>
    <div class=""footer"">
      <p><strong>{HospitalName}</strong> &bull; {HospitalAddress}<br>
      Phone: {HospitalPhone} &bull; Email: {HospitalEmail}</p>
    </div>
  </div>
</body>
</html>";

            var text = $@"DEAR {ownerName},

This is an official clinical status update from {HospitalName} regarding {patientName} ({species}, {breed}).

CURRENT STATUS:
- Patient: {patientName}
- Species/Breed: {species} ({breed})
- Assigned Ward: {wardName}
- Admission Status: {admissionStatus}
- Clinical Status: {statusDisplay}

Our veterinary team is continuing treatment protocols and daily care checks. If you wish to speak with our clinical staff or schedule a visit, feel free to call our reception at {HospitalPhone}.

Sincerely,
The Attending Clinical Staff at {HospitalName}
{HospitalAddress}";

            return new EmailTemplateResult(subject, html.Trim(), text.Trim());
        }
    }
}
