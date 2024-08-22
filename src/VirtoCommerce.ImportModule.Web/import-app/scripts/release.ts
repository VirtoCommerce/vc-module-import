import { release } from "@vc-shell/release-config";
import { sync } from "cross-spawn";

release({
  packages: [".", "src/api_client", "src/modules"],
  toTag: (version) => `v${version}`,
  bumpVersion: async (pkgName, pkgVersion) => {
    const bumpArgs = ["workspace", pkgName, "version", pkgVersion, "--deferred"];
    await sync("yarn", bumpArgs);

    const versionApplyArgs = ["version", "apply", "--all"];
    await sync("yarn", versionApplyArgs);
  },
  generateChangelog: async () => {},
});
