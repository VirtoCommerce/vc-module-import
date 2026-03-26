import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

const __dirname = dirname(fileURLToPath(import.meta.url));

export default getDynamicModuleConfiguration({
  base: "/apps/import-app/",
  entry: "./src/modules/index.ts",
  compatibility: {
    framework: "^2.0.0",
  },
  resolve: {
    alias: {
      "/assets/empty.png": resolve(__dirname, "../../public/assets/empty.png"),
    },
  },
});
