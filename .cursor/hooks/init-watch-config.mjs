/**
 * 生成 watch-config.json（Windows Hook spawn EPERM 时，用终端监听代替）
 * 在项目根目录运行: node .cursor/hooks/init-watch-config.mjs
 */
import fs from "fs/promises";
import path from "path";
import os from "os";

const workspaceRoot = process.cwd();
const projectsRoot = path.join(
  os.homedir(),
  ".cursor",
  "projects",
);

const folderName = path.basename(workspaceRoot).toLowerCase();
let best = null;

try {
  const entries = await fs.readdir(projectsRoot, { withFileTypes: true });
  for (const ent of entries) {
    if (!ent.isDirectory()) continue;
    const transcriptsDir = path.join(
      projectsRoot,
      ent.name,
      "agent-transcripts",
    );
    try {
      await fs.access(transcriptsDir);
    } catch {
      continue;
    }
    const slug = ent.name.toLowerCase();
    let score = 1;
    if (slug.includes(folderName.replace(/\s+/g, "-"))) score += 2;
    if (slug.includes(folderName.replace(/\s+/g, ""))) score += 1;
    if (!best || score > best.score) {
      best = { projectSlug: ent.name, transcriptsDir, score };
    }
  }
} catch (e) {
  console.error("无法扫描 Cursor projects 目录:", e.message);
  process.exit(1);
}

if (!best) {
  console.error(
    "未找到 agent-transcripts 目录。请手动编辑 .cursor/hooks/watch-config.json",
  );
  process.exit(1);
}

const config = {
  workspaceRoot,
  transcriptsDir: best.transcriptsDir,
  projectSlug: best.projectSlug,
};
const outPath = path.join(workspaceRoot, ".cursor", "hooks", "watch-config.json");
await fs.mkdir(path.dirname(outPath), { recursive: true });
await fs.writeFile(outPath, JSON.stringify(config, null, 2), "utf8");
console.log("已写入:", outPath);
console.log(JSON.stringify(config, null, 2));
