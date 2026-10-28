import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "D:/Resolve AI/Resolve-AI.API/outputs/ai-meeting-api-pricing";
const workbook = Workbook.create();
const sheet = workbook.worksheets.add("API & Pricing");
sheet.showGridLines = false;
sheet.tabColor = "#1F4E78";

const navy = "#1F4E78";
const blue = "#D9EAF7";
const pale = "#F6F9FC";
const green = "#E2F0D9";
const amber = "#FFF2CC";
const gray = "#6B7280";
const border = "#C9D5E3";
const font = "Arial";

function styleHeader(range, fill = navy) {
  range.format = {
    fill,
    font: { name: font, size: 10, bold: true, color: "#FFFFFF" },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    wrapText: true,
    borders: { preset: "all", style: "thin", color: "#FFFFFF" },
  };
}

function styleBody(range) {
  range.format = {
    font: { name: font, size: 10, color: "#1F2937" },
    verticalAlignment: "top",
    wrapText: true,
    borders: { preset: "all", style: "thin", color: border },
  };
}

// Title and scope
sheet.mergeCells("A2:G2");
sheet.getRange("A2").values = [["AI Meeting App — API, Feature Options & Pricing Guide"]];
sheet.getRange("A2").format = {
  font: { name: font, size: 16, bold: true, color: navy },
  verticalAlignment: "center",
};
sheet.getRange("A2").format.rowHeight = 28;

sheet.mergeCells("A3:G3");
sheet.getRange("A3").values = [["Scope: external APIs and usage-based services only. Hosting, domain, and database are intentionally excluded."]];
sheet.getRange("A3").format = {
  font: { name: font, size: 10, italic: true, color: gray },
  verticalAlignment: "center",
};

// Feature choices
sheet.mergeCells("A5:G5");
sheet.getRange("A5").values = [["1. Feature Options and Cost Impact"]];
sheet.getRange("A5").format = {
  fill: blue,
  font: { name: font, size: 12, bold: true, color: navy },
  verticalAlignment: "center",
};

sheet.getRange("A6:G6").values = [[
  "Area", "Option A", "Cost impact", "Option B", "Cost impact", "Recommended starting point", "Plan placement"
]];
styleHeader(sheet.getRange("A6:G6"));

const featureRows = [
  ["Meeting input", "Manual audio/video upload", "Lower", "Live recording + upload", "Medium", "Start with manual upload", "All plans"],
  ["Speaker names", "Speaker 1 / Speaker 2; user renames", "Lower", "Automatic diarization / meeting metadata", "Medium to high", "Manual labels first", "Starter and above"],
  ["Translation", "Translated text", "Lower", "Translated text + AI voice audio", "Higher", "Text first; voice in Pro", "Text: Starter | Voice: Pro"],
  ["Meeting platforms", "Manual upload only", "Lower", "Zoom / Teams / Webex auto-import", "Higher", "Integrations later", "Team / Enterprise"],
  ["Users", "Individual account", "Lower", "Team accounts, roles, sharing", "Medium", "Individual first", "Team / Enterprise"],
  ["Meeting retention", "Delete recording after selected period", "Lower", "Keep recording indefinitely", "Higher", "Offer retention choices", "Pro / Enterprise"],
];
sheet.getRange(`A7:G${6 + featureRows.length}`).values = featureRows;
styleBody(sheet.getRange(`A7:G${6 + featureRows.length}`));
sheet.getRange(`C7:C${6 + featureRows.length}`).format.fill = pale;
sheet.getRange(`E7:E${6 + featureRows.length}`).format.fill = pale;
sheet.getRange(`F7:F${6 + featureRows.length}`).format.fill = green;

// External APIs
const apiStart = 15;
sheet.mergeCells(`A${apiStart}:G${apiStart}`);
sheet.getRange(`A${apiStart}`).values = [["2. External APIs / Services Required"]];
sheet.getRange(`A${apiStart}`).format = {
  fill: blue,
  font: { name: font, size: 12, bold: true, color: navy },
  verticalAlignment: "center",
};
sheet.getRange(`A${apiStart + 1}:G${apiStart + 1}`).values = [[
  "Capability", "API / Service Type", "What it does", "When needed", "Usage driver", "Paid status", "Notes"
]];
styleHeader(sheet.getRange(`A${apiStart + 1}:G${apiStart + 1}`));

const apiRows = [
  ["Audio transcription", "Speech-to-text API", "Audio/video speech becomes original transcript", "Core feature", "Audio minutes", "Paid usage", "Required for every processed meeting"],
  ["Speaker separation", "Diarization transcription API", "Separates different voices into speaker labels", "When multiple people speak", "Audio minutes", "Paid usage", "Names still need user confirmation or meeting metadata"],
  ["Text translation", "Text AI / translation API", "Original transcript becomes Urdu, English, Arabic, or selected language", "When user selects translation", "Transcript length / tokens", "Paid usage", "Preserve speaker labels in the translated result"],
  ["Summary + action items", "Text AI API", "Creates summary, decisions, key points, and assigned actions", "After transcript is ready", "Transcript length / tokens", "Paid usage", "Can be one combined request or separate requests"],
  ["Translated voice", "Text-to-speech API", "Translated text becomes downloadable/playable AI audio", "Pro feature", "Text characters or generated audio", "Paid usage", "Extra cost beyond text translation"],
  ["Zoom integration", "Zoom API + webhooks", "Imports recordings automatically", "Later integration", "Account/plan and API usage", "Depends on Zoom plan", "Not required for manual upload MVP"],
  ["Teams integration", "Microsoft Graph API", "Imports Teams recordings/transcripts where permitted", "Later integration", "Microsoft 365 tenant and API access", "Depends on M365 plan", "Requires permissions and tenant approval"],
  ["Webex integration", "Webex API + webhooks", "Imports Webex recordings automatically", "Later integration", "Account/plan and API usage", "Depends on Webex plan", "Not required for manual upload MVP"],
  ["Notifications", "Firebase Cloud Messaging", "Sends transcript-ready or action-item notifications", "Android/mobile phase", "Notification volume", "No-cost service", "Useful later for Android app"],
  ["Payments", "Payment gateway API", "Collects subscriptions from customers", "Only if paid plans launch", "Per successful transaction", "Transaction fee", "Not required for an internal/private tool"],
];
const apiEnd = apiStart + 1 + apiRows.length;
sheet.getRange(`A${apiStart + 2}:G${apiEnd}`).values = apiRows;
styleBody(sheet.getRange(`A${apiStart + 2}:G${apiEnd}`));
sheet.getRange(`F${apiStart + 2}:F${apiEnd}`).format.fill = amber;

// Pricing structure
const priceStart = apiEnd + 3;
sheet.mergeCells(`A${priceStart}:G${priceStart}`);
sheet.getRange(`A${priceStart}`).values = [["3. Suggested Product Pricing Structure — Example Only"]];
sheet.getRange(`A${priceStart}`).format = {
  fill: blue,
  font: { name: font, size: 12, bold: true, color: navy },
  verticalAlignment: "center",
};
sheet.mergeCells(`A${priceStart + 1}:G${priceStart + 1}`);
sheet.getRange(`A${priceStart + 1}`).values = [["Set exact PKR prices only after testing API usage per meeting. Charge by plan with included minutes, then charge for extra usage."]];
sheet.getRange(`A${priceStart + 1}`).format = {
  font: { name: font, size: 10, italic: true, color: gray },
  verticalAlignment: "center",
};

sheet.getRange(`A${priceStart + 2}:G${priceStart + 2}`).values = [[
  "Plan", "Who it is for", "Included capabilities", "Included minutes", "When extra charge applies", "Price display", "Purpose"
]];
styleHeader(sheet.getRange(`A${priceStart + 2}:G${priceStart + 2}`));

const pricingRows = [
  ["Free", "New individual", "Transcript + basic summary", "Small monthly limit", "When minute limit ends", "PKR 0/month", "Try product"],
  ["Starter", "Individual user", "Transcript + summary + action items + translated text", "Set monthly minute package", "Extra transcription / translation minutes", "PKR [TBD]/month", "Regular meetings"],
  ["Pro", "Heavy individual", "Starter + translated audio + download + search", "Larger minute package", "Extra transcription, translation, or voice minutes", "PKR [TBD]/month", "Premium usage"],
  ["Team", "Company / team", "Pro + sharing + roles + shared minutes", "Shared organisation minute pool", "Extra shared minutes or user seats", "PKR [TBD]/user/month", "Collaboration"],
  ["Enterprise", "Large company", "Team + integrations + custom controls", "Custom package", "Contract-based usage", "Custom quote", "Security / integrations"],
];
const priceEnd = priceStart + 2 + pricingRows.length;
sheet.getRange(`A${priceStart + 3}:G${priceEnd}`).values = pricingRows;
styleBody(sheet.getRange(`A${priceStart + 3}:G${priceEnd}`));
sheet.getRange(`F${priceStart + 3}:F${priceEnd}`).format.fill = amber;

// Billing logic + sources
const billStart = priceEnd + 3;
sheet.mergeCells(`A${billStart}:G${billStart}`);
sheet.getRange(`A${billStart}`).values = [["4. How Billing Works"]];
sheet.getRange(`A${billStart}`).format = {
  fill: blue,
  font: { name: font, size: 12, bold: true, color: navy },
  verticalAlignment: "center",
};

const billingRows = [
  ["1", "User uploads a meeting", "No AI charge until processing starts"],
  ["2", "Audio is transcribed", "Charge relates to the audio minutes processed"],
  ["3", "Transcript is translated / summarized", "Charge relates to text length and selected AI model"],
  ["4", "Translated voice is generated", "Extra charge relates to generated speech/text volume"],
  ["5", "User exceeds monthly included minutes", "Apply extra-minute or usage-pack charge"],
];
sheet.getRange(`A${billStart + 1}:C${billStart + 1}`).values = [["Step", "What happens", "Why cost changes"]];
styleHeader(sheet.getRange(`A${billStart + 1}:C${billStart + 1}`));
sheet.getRange(`A${billStart + 2}:C${billStart + 6}`).values = billingRows;
styleBody(sheet.getRange(`A${billStart + 2}:C${billStart + 6}`));

sheet.mergeCells(`A${billStart + 8}:G${billStart + 8}`);
sheet.getRange(`A${billStart + 8}`).values = [["Sources: OpenAI Audio API and pricing — https://platform.openai.com/docs/api-reference/audio/voice-consent-list?lang=curl | https://developers.openai.com/api/docs/pricing  |  Firebase Cloud Messaging — https://firebase.google.com/products/cloud-messaging"]];
sheet.getRange(`A${billStart + 8}`).format = {
  font: { name: font, size: 8, italic: true, color: gray },
  verticalAlignment: "center",
  wrapText: true,
};

// Column widths and row sizing
const widths = [19, 31, 17, 35, 17, 29, 26];
for (let i = 0; i < widths.length; i++) sheet.getCell(0, i).format.columnWidth = widths[i];
sheet.getRange(`A1:G${billStart + 8}`).format.font = { name: font, size: 10, color: "#1F2937" };
sheet.getRange("A6:G6").format.rowHeight = 30;
sheet.getRange(`A${apiStart + 1}:G${apiStart + 1}`).format.rowHeight = 30;
sheet.getRange(`A${priceStart + 2}:G${priceStart + 2}`).format.rowHeight = 30;
sheet.getRange(`A${billStart + 1}:C${billStart + 1}`).format.rowHeight = 24;
sheet.getRange(`A7:G${6 + featureRows.length}`).format.rowHeight = 34;
sheet.getRange(`A${apiStart + 2}:G${apiEnd}`).format.rowHeight = 43;
sheet.getRange(`A${priceStart + 3}:G${priceEnd}`).format.rowHeight = 38;
sheet.getRange(`A${billStart + 2}:C${billStart + 6}`).format.rowHeight = 28;
sheet.getRange(`A${billStart + 8}:G${billStart + 8}`).format.rowHeight = 32;

sheet.freezePanes.freezeRows(6);

workbook.recalculate();

const check = await workbook.inspect({
  kind: "table",
  range: `A2:G${billStart + 8}`,
  include: "values,formulas",
  tableMaxRows: 60,
  tableMaxCols: 7,
});
console.log(check.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
  options: { useRegex: true, maxResults: 50 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const preview = await workbook.render({ sheetName: "API & Pricing", range: `A1:G${billStart + 8}`, scale: 1.25, format: "png" });
await fs.writeFile(`${outputDir}/api-pricing-preview.png`, new Uint8Array(await preview.arrayBuffer()));

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/AI_Meeting_App_API_and_Pricing_Guide.xlsx`);
