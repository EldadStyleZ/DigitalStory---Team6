---
name: finish-feature
description: Use when a feature or fix in this Unity project seems finished, before saying it is done, committing, pushing or opening a pull request.
---

# Finish a feature

1. **Verify.** Run every check under Verification in `AGENTS.md`, after your last code change. If one fails and you cannot fix it, stop and report it. Do not push.
2. **Review your diff** against `docs/CODING_STANDARDS.md`:
   - Game rules are in Logic, and tunable values are in Data.
   - Every game rule has a test. If one is missing, write it now and run it before you push.
   - Renamed serialized fields have `[FormerlySerializedAs]`.
   - No temporary logs remain.
   - Only expected scenes and prefabs changed, and new assets have their `.meta` files.
   - No secrets or personal paths.
3. **Update `FEATURES.md` on your branch.** Set the status to `Done`, fill in `Verified by` and clear `Holding`. Merging the pull request makes that true and releases your scenes. If part of the feature still waits on a held file, keep the status `In progress` and note what is left.
4. **Open the pull request** into `main`. State what changed, why, how it was verified and what was not verified. A teammate reviews and merges it.
5. **Report** the checks you ran and what they showed, the link, and anything unverified.
