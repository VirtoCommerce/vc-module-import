import { getApplicationConfiguration } from "@vc-shell/config-generator";
import { resolve } from "node:path";

export default getApplicationConfiguration({
  resolve: {
    alias: {
      "@virtocommerce/import-app": resolve("src/modules/index.ts"),
    },
  },
});
