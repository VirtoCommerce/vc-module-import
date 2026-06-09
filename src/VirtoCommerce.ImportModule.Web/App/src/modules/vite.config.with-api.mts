import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

const __dirname = dirname(fileURLToPath(import.meta.url));

// vite.config sits at App/src/modules/, .NET module root is three levels up.
const moduleRoot = resolve(__dirname, "../../..");

export default getDynamicModuleConfiguration({
  entry: "./src/modules/index.ts",
  appId: "vendor-portal",
  moduleRoot,
  remoteName: "VirtoCommerce.Import",
  resolve: {
    alias: {
      "/assets/empty.png": resolve(__dirname, "../../public/assets/empty.png"),
    },
  },
});
