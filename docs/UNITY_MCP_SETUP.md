# Unity MCP setup (once per person)

Quit Unity, then from the project folder:

```bash
npx unity-mcp-cli@1.5.0 setup-mcp claude-code . --transport stdio --no-pin
npx unity-mcp-cli@1.5.0 setup-mcp gemini . --transport stdio --no-pin
```

A fresh clone has no plugin settings, so the plugin starts in cloud mode. Before first opening the project, create `UserSettings/AI-Game-Developer-Config.json` with the port from `.mcp.json`:

```json
{
  "host": "http://localhost:PORT",
  "connectionMode": "Custom",
  "transportMethod": "stdio",
  "authOption": "none",
  "keepConnected": true,
  "keepServerRunning": false,
  "generateSkillFiles": false,
  "skillAutoGenerate": { "claude-code": false },
  "skillsPath": ".claude/skills"
}
```

- Local only: never `unity-mcp-cli login`, `--token`, `--oauth` or `--regenerate-key`.
- Always run as `npx unity-mcp-cli@1.5.0`. If the plugin offers an update, choose Skip Version (version changes are a team decision).
- Keep transport on stdio and skill generation off.
- Keep the project out of synced folders (iCloud Desktop/Documents): sync leaves ` 2` copies that break compilation.

Runtime gotchas are in `AGENTS.md` under Unity MCP.
