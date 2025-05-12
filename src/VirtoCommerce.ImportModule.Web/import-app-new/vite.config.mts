import { getApplicationConfiguration } from "@vc-shell/config-generator";
import { resolve } from "node:path";

export default getApplicationConfiguration({
  resolve: {
    alias: {
      "@virtocommerce/import-module": resolve("src/modules/index.ts"),
    },
  },
});
