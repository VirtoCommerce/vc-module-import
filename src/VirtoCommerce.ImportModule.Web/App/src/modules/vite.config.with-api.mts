import { resolve, dirname } from "node:path";
import { readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";
import type { Plugin } from "vite";

const __dirname = dirname(fileURLToPath(import.meta.url));

/**
 * Preserve the standalone index.html across the MF build.
 *
 * The standalone build (build:app) produces index.html first, then the MF
 * build (build:modules-bundle) overwrites it with an MF-bootstrapped version
 * that cannot run without a host. This plugin saves the standalone HTML
 * before the build and restores it after all files are written.
 */
function preserveStandaloneHtml(): Plugin {
  let savedHtml: Buffer | null = null;
  const htmlPath = resolve("dist", "index.html");

  return {
    name: "preserve-standalone-html",
    buildStart() {
      try {
        savedHtml = readFileSync(htmlPath);
      } catch {
        savedHtml = null;
      }
    },
    closeBundle() {
      if (savedHtml) {
        writeFileSync(htmlPath, savedHtml);
      }
    },
  };
}

export default getDynamicModuleConfiguration({
  base: "/apps/import-app/",
  entry: "./src/modules/index.ts",
  compatibility: {
    framework: "^2.0.0",
  },
  build: {
    emptyOutDir: false,
  },
  plugins: [preserveStandaloneHtml()],
  resolve: {
    alias: {
      "/assets/empty.png": resolve(__dirname, "../../public/assets/empty.png"),
    },
  },
});
