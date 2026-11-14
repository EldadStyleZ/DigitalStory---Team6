using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DigitalStory.View;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>Checks, in Play mode, the map requests: no overlaps, nothing on water, spread, bridges, grass, nest trees,
    /// the edge forest and the beekeeper's strike bones. Writes Logs/RequestCheck.txt and the console.</summary>
    public static class RequestCheckMenu
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [MenuItem("Tools/DigitalStory/Check My Requests")]
        public static void Check()
        {
            var report = new StringBuilder();
            int fails = 0;
            void Line(bool ok, string text) { if (!ok) fails++; report.AppendLine((ok ? "PASS  " : "FAIL  ") + text); }
            void Info(string text) => report.AppendLine("      " + text);

            if (!Application.isPlaying) { Debug.LogWarning("Run Check My Requests while playing."); return; }

            var root = Object.FindFirstObjectByType<HiveDefenseRoot>();
            var scenery = Object.FindFirstObjectByType<SceneryView>();
            var props = ((Transform[])typeof(SceneryView).GetField("_props", Any).GetValue(scenery)).Where(p => p != null && p.gameObject.activeInHierarchy).ToList();
            var foot = scenery.Footprints;
            var all = (Transform[])typeof(SceneryView).GetField("_props", Any).GetValue(scenery);
            float Foot(Transform t) => foot[System.Array.IndexOf(all, t)];
            float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

            // Overlap: footprint spacing and real model boxes.
            int spacing = 0, boxes = 0;
            var bounds = props.Select(p => { var r = p.GetComponentsInChildren<Renderer>(); var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds); return b; }).ToList();
            for (int i = 0; i < props.Count; i++)
            for (int j = i + 1; j < props.Count; j++)
            {
                if (Flat(props[i].position, props[j].position) < Foot(props[i]) + Foot(props[j]) - 0.05f) spacing++;
                Bounds a = bounds[i], b = bounds[j];
                float ox = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                float oz = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                if (ox > 0f && oz > 0f)
                {
                    float small = Mathf.Min(a.size.x * a.size.z, b.size.x * b.size.z);
                    if (ox * oz > 0.15f * small) { boxes++; if (boxes <= 5) Info($"overlap: {props[i].name} @ {props[i].position:F0} with {props[j].name} @ {props[j].position:F0}"); }
                }
            }
            Line(spacing == 0, $"{props.Count} objects placed; pairs closer than their footprints: {spacing}");
            Line(boxes == 0, $"pairs whose models overlap: {boxes}");

            // Objects against hives and nests.
            var hives = ((System.Array)typeof(HiveDefenseRoot).GetField("_hives", Any).GetValue(root)).Cast<Component>().Select(h => h.transform.position).ToList();
            var nests = GameObject.Find("Nests").transform.Cast<Transform>().Select(n => n.position).ToList();
            var spawns = ((Transform[])typeof(HiveDefenseRoot).GetField("_spawnPoints", Any).GetValue(root)).Select(s => s.position).ToList();
            int crowd = props.Count(p => hives.Any(h => Flat(p.position, h) < Foot(p) + 3f) || nests.Any(n => Flat(p.position, n) < Foot(p) + 3f));
            Line(crowd == 0, $"objects sitting on a hive or nest tree: {crowd}");

            // Water.
            int wet = props.Count(p => RiverBridge.Nearness(p.position, Foot(p)) > 0f);
            int wetNests = nests.Count(n => RiverBridge.Nearness(n, 1.5f) > 0f);
            int wetHives = hives.Count(h => RiverBridge.Nearness(h, 2f) > 0f);
            Line(wet == 0, $"objects touching the river: {wet}");
            Line(wetNests + wetHives == 0, $"nests/hives on the river: {wetNests + wetHives}");

            // Spread over 100 x 100.
            float minX = props.Min(p => p.position.x), maxX = props.Max(p => p.position.x), minZ = props.Min(p => p.position.z), maxZ = props.Max(p => p.position.z);
            var cells = new int[4, 4];
            foreach (var p in props) cells[Mathf.Clamp((int)((p.position.x + 100f) / 50f), 0, 3), Mathf.Clamp((int)((p.position.z + 100f) / 50f), 0, 3)]++;
            int empty = 0; for (int x = 0; x < 4; x++) for (int z = 0; z < 4; z++) if (cells[x, z] == 0) empty++;
            Line(maxX - minX > 150f && maxZ - minZ > 150f && empty <= 2, $"spread x {minX:F0}..{maxX:F0}, z {minZ:F0}..{maxZ:F0}; empty 50 m squares of 16: {empty}");

            // Bridges.
            var bridges = Object.FindObjectsByType<RiverBridge>(FindObjectsSortMode.None).ToList();
            var mids = bridges.Select(b => b.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(b.transform.position, Vector3.zero), (acc, r) => { acc.Encapsulate(r.bounds); return acc; }).center).ToList();
            int close = 0; for (int i = 0; i < mids.Count; i++) for (int j = i + 1; j < mids.Count; j++) if (Flat(mids[i], mids[j]) < 25f) close++;
            Line(close == 0, $"{bridges.Count} bridges; pairs closer than 25 m: {close}");
            int stuck = 0;
            for (int i = 0; i < spawns.Count; i++)
            {
                Vector3 hive = hives[i % hives.Count];
                Vector3 at = spawns[i];
                bool reached = false; string why = "walked 200 m without arriving";
                for (int step = 0; step < 400 && !reached; step++)
                {
                    Vector3 next = RiverBridge.Route(at, hive);
                    Vector3 move = next - at; move.y = 0f;
                    if (Flat(at, hive) < 1f) { reached = true; break; }
                    at += move.normalized * Mathf.Min(0.5f, move.magnitude);
                    if ((bool)typeof(RiverBridge).GetMethod("IsWet", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { at }) && !OnAnyDeck(bridges, at)) { why = "in water"; break; }
                }
                if (!reached) { stuck++; Info($"spawn {i} @ {spawns[i]:F0} to hive @ {hive:F0} stuck at {at:F0}: {why}; route says {RiverBridge.Route(at, hive):F0}; bridges at {string.Join(" ", mids.Select(m => m.ToString("F0")))}"); }
            }
            Line(stuck == 0, $"beekeepers that cannot walk to their hive over a bridge: {stuck} of {spawns.Count}");

            // Grass.
            var grass = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == "GrassField");
            float cover = grass != null ? (float)grass.GetType().GetField("_coverHalfSize", Any).GetValue(grass) : 0f;
            Line(cover > 100f, $"grass reaches {cover:F0} m from the middle");

            // Nest trees.
            var heights = GameObject.Find("Nests").GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("WaspNestTree")).Select(t => { var r = t.GetComponentsInChildren<Renderer>(); var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds); return b.size.y; }).ToList();
            Line(heights.Count > 0 && heights.All(h => h > 8f && h < 10f), $"nest trees tall: {string.Join(", ", heights.Select(h => h.ToString("F1")))} m (pine 9 m)");

            // Edge forest.
            var forest = GameObject.Find("Set Dressing")?.transform.Find("Forest");
            var trees = forest != null ? forest.Cast<Transform>().Where(t => t.name.Contains("Pine") || t.name.Contains("Dead")).Select(t => t.position).ToList() : new List<Vector3>();
            int inside = trees.Count(t => Mathf.Max(Mathf.Abs(t.x), Mathf.Abs(t.z)) < 100f);
            int gaps = 0, wetEdge = 0; var gapList = new List<string>();
            for (float s = -102f; s <= 102f; s += 2f)
            foreach (var p in new[] { new Vector3(s, 0, 103f), new Vector3(s, 0, -103f), new Vector3(103f, 0, s), new Vector3(-103f, 0, s) })
            {
                if (RiverBridge.Nearness(p, 6f) > 0f) { wetEdge++; continue; }
                if (!trees.Any(t => Flat(t, p) < 5f)) { gaps++; if (gaps <= 60) gapList.Add($"({p.x:F0},{p.z:F0}) wet{RiverBridge.Nearness(p, 8f):F2}"); }
            }
            Line(trees.Count > 300 && gaps < 6, $"edge forest: {trees.Count} trees, {inside} inside the play area, open spots along the edge: {gaps} (river spots skipped: {wetEdge})");
            if (gapList.Count > 0) Info("open: " + string.Join(" ", gapList));
            var ter = Terrain.activeTerrain; Info($"terrain from {ter.GetPosition():F0} size {ter.terrainData.size:F0}");
            float half = (float)Resources.FindObjectsOfTypeAll<ScriptableObject>().First(o => o.GetType().Name == "CoreMoveConfig").GetType().GetProperty("PlayAreaHalfSize").GetValue(Resources.FindObjectsOfTypeAll<ScriptableObject>().First(o => o.GetType().Name == "CoreMoveConfig"));
            Line(Mathf.Approximately(half, 100f), $"the swarm is held inside {half:F0} m, so it cannot go into the forest");

            // Strike bones on the beekeepers.
            foreach (var path in new[] { "Assets/_Project/Prefabs/F010/F010StickBeekeeper.prefab", "Assets/_Project/Prefabs/F010/F010SmokerBeekeeper.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var names = prefab.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
                Line(names.Contains("Spine") && names.Contains("RightUpperArm") && names.Contains("RightLowerArm"), $"{prefab.name} has the bones the strike moves");
            }

            // Collecting a nest: stand on the first open one and play its combo, through the session's own rules.
            var field = root.Session.Nests;
            var nestArray = (System.Array)typeof(DigitalStory.Logic.NestField).GetField("_nests", Any).GetValue(field);
            int open = -1;
            for (int i = 0; i < nestArray.Length; i++) if (field[i].IsOpen) { open = i; break; }
            if (open < 0) Line(false, "no nest is open to collect");
            else
            {
                var nestView = GameObject.Find("Nests").transform.GetChild(open).position;
                Info($"nest {open + 1} logic at {field[open].Position:F1}, its tree at {nestView:F1}, open nests {Enumerable.Range(0, nestArray.Length).Count(i => field[i].IsOpen)}");
                field.Tick(field[open].Position, true);
                bool shown = field.ActiveNest == open;
                var last = DigitalStory.Logic.NestComboStep.Ignored;
                foreach (int hole in field[open].Combo.ToArray()) last = field.PlayNote(hole);
                Line(shown && last == DigitalStory.Logic.NestComboStep.Completed, $"standing on nest {open + 1} shows its combo: {shown}; playing it collects it: {last}");
            }

            report.Insert(0, $"Request check — {fails} failed\n");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/RequestCheck.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static bool OnAnyDeck(List<RiverBridge> bridges, Vector3 at)
        {
            var onDeck = typeof(RiverBridge).GetMethod("OnDeck", Any);
            return bridges.Any(b => (bool)onDeck.Invoke(b, new object[] { at }));
        }
    }
}
