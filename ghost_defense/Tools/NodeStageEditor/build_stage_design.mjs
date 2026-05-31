import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "C:/AI/Ghost/ghost_defense/ghost_defense/Tools/NodeStageEditor";
await fs.mkdir(outputDir, { recursive: true });

const workbook = Workbook.create();
const nodeSheet = workbook.worksheets.add("NodeStage");
const monsterSheet = workbook.worksheets.add("MonsterBase");

nodeSheet.showGridLines = false;
monsterSheet.showGridLines = false;

nodeSheet.getRange("A1:H1").merge();
nodeSheet.getRange("A1").values = [["NodeStage - 노드 배치/연결 CSV"]];
nodeSheet.getRange("A2:H2").merge();
nodeSheet.getRange("A2").values = [["HTML 에디터에서 최종 CSV로 내보낼 기준 컬럼입니다. nodeId와 nextNodeIds는 001 형식 텍스트로 관리하세요."]];
nodeSheet.getRange("A4:H4").values = [[
  "stageId", "layer", "nodeId", "nodeType", "dungeonTemplateId", "monsterId", "nodeMultiplier", "nextNodeIds"
]];
nodeSheet.getRange("A5:H5").values = [[
  "스테이지 ID", "레이어", "노드 ID", "노드 타입", "던전 템플릿 ID", "몬스터 ID", "노드 배율", "다음 노드 ID들"
]];
nodeSheet.getRange("A6:H13").values = [
  [1, 1, 1, "Normal", "Up_001", "SkeletonCactus", 1.0, "002|003"],
  [1, 2, 2, "Normal", "Down_001", "SkeletonCactus", 1.1, "004"],
  [1, 2, 3, "CardShop", "", "", "", "005"],
  [1, 3, 4, "Merchant", "", "", "", "006"],
  [1, 3, 5, "Hard", "Down_002", "SkeletonCactus", 1.5, "006"],
  [1, 4, 6, "Normal", "Up_002", "SkeletonCactus", 1.8, "007"],
  [1, 5, 7, "EventA", "", "", "", "008"],
  [1, 6, 8, "Boss", "Up_004", "SkeletonCactus", 4.0, ""]
];

monsterSheet.getRange("A1:J1").merge();
monsterSheet.getRange("A1").values = [["MonsterBase - SO_MonsterData 기본값 관리"]];
monsterSheet.getRange("A2:J2").merge();
monsterSheet.getRange("A2").values = [["SO_MonsterData를 유지할 때 사람이 관리할 기본값입니다. 노드에서는 monsterId만 참조하고 체력은 배율로 계산합니다."]];
monsterSheet.getRange("A4:J4").values = [[
  "monsterId", "monsterName", "baseHp", "weaknessDamageType", "weaknessAttackStyle", "firstGold", "repeatGold", "firstDiamond", "repeatDiamond", "visualKey"
]];
monsterSheet.getRange("A5:J5").values = [[
  "몬스터 ID", "몬스터 이름", "기본 체력", "약점 속성", "약점 공격 방식", "첫 클리어 골드", "반복 골드", "첫 클리어 다이아", "반복 다이아", "표시 리소스 키"
]];
monsterSheet.getRange("A6:J9").values = [
  ["SkeletonCactus", "해골 선인장", 1000, "None", "None", 300, 50, 50, 1, "Monster_SkeletonCactus"],
  ["GhostSoldier", "유령 병사", 1600, "Holy", "Ranged", 320, 55, 50, 1, "Monster_GhostSoldier"],
  ["StoneGolem", "돌 골렘", 2600, "Magic", "Melee", 360, 60, 55, 1, "Monster_StoneGolem"],
  ["BossWraith", "망령 보스", 9000, "Holy", "Burst", 800, 120, 120, 5, "Monster_BossWraith"]
];

for (const sheet of [nodeSheet, monsterSheet]) {
  sheet.freezePanes.freezeRows(5);
  sheet.getRange("A1:J1").format = {
    fill: "#1F2937",
    font: { bold: true, color: "#FFFFFF", size: 15 },
    horizontalAlignment: "Left",
    verticalAlignment: "Center"
  };
  sheet.getRange("A2:J2").format = {
    fill: "#EEF2FF",
    font: { color: "#374151" },
    wrapText: true
  };
  sheet.getRange("A4:J4").format = {
    fill: "#2563EB",
    font: { bold: true, color: "#FFFFFF" },
    horizontalAlignment: "Center"
  };
  sheet.getRange("A5:J5").format = {
    fill: "#DBEAFE",
    font: { bold: true, color: "#1F2937" },
    horizontalAlignment: "Center"
  };
}

nodeSheet.getRange("A4:H13").format = { border: { color: "#CBD5E1", style: "Continuous" } };
monsterSheet.getRange("A4:J9").format = { border: { color: "#CBD5E1", style: "Continuous" } };
nodeSheet.getRange("C6:C205").format.numberFormat = "000";
nodeSheet.getRange("H6:H205").format.numberFormat = "@";
nodeSheet.getRange("G6:G205").format.numberFormat = "0.00";
monsterSheet.getRange("C6:C205").format.numberFormat = "#,##0";
monsterSheet.getRange("F6:I205").format.numberFormat = "#,##0";

const nodeWidths = ["A", "B", "C", "D", "E", "F", "G", "H"];
const nodeWidthValues = [90, 80, 115, 155, 175, 170, 125, 150];
nodeWidths.forEach((col, i) => {
  nodeSheet.getRange(`${col}1:${col}205`).format.columnWidthPx = nodeWidthValues[i];
});

const monsterWidths = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];
const monsterWidthValues = [150, 140, 100, 165, 165, 120, 110, 130, 120, 180];
monsterWidths.forEach((col, i) => {
  monsterSheet.getRange(`${col}1:${col}205`).format.columnWidthPx = monsterWidthValues[i];
});

nodeSheet.getRange("A1:H1").format.rowHeightPx = 30;
monsterSheet.getRange("A1:J1").format.rowHeightPx = 30;
nodeSheet.getRange("A2:H2").format.rowHeightPx = 38;
monsterSheet.getRange("A2:J2").format.rowHeightPx = 38;
nodeSheet.getRange("C6:C205").format.horizontalAlignment = "Center";
nodeSheet.getRange("D6:D205").format.horizontalAlignment = "Left";

try {
  nodeSheet.tables.add("A4:H13", true, "NodeStageTable");
  monsterSheet.tables.add("A4:J9", true, "MonsterBaseTable");
} catch {
}

try {
  nodeSheet.getRange("D6:D205").dataValidation = { rule: { type: "list", values: ["Normal", "Hard", "Boss", "EventA", "EventB", "EventC", "CardShop", "Merchant"] } };
  nodeSheet.getRange("E6:E205").dataValidation = { rule: { type: "list", values: ["Up_001", "Up_002", "Up_003", "Up_004", "Down_001", "Down_002", "Down_003"] } };
  nodeSheet.getRange("F6:F205").dataValidation = { rule: { type: "list", formula1: "MonsterBase!$A$6:$A$205" } };
} catch {
}

const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 50 },
  maxChars: 1000
});
console.log(errorScan.ndjson);

const nodePreview = await workbook.render({ sheetName: "NodeStage", range: "A1:H13", scale: 1, format: "png" });
await fs.writeFile(path.join(outputDir, "TXT_NodeStageDesign_NodeStage.png"), new Uint8Array(await nodePreview.arrayBuffer()));
const monsterPreview = await workbook.render({ sheetName: "MonsterBase", range: "A1:J9", scale: 1, format: "png" });
await fs.writeFile(path.join(outputDir, "TXT_NodeStageDesign_MonsterBase.png"), new Uint8Array(await monsterPreview.arrayBuffer()));

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(path.join(outputDir, "TXT_NodeStageDesign.xlsx"));
