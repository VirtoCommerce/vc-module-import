import defaultConfig, { content } from "@vc-shell/framework/tailwind.config";

export default {
  prefix: "tw-",
  content: [
    ...content,
    "./src/pages/**/*.{vue,js,ts,jsx,tsx}",
    "./src/composables/**/*.{vue,js,ts,jsx,tsx}",
    "./src/modules/import/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: defaultConfig.theme,
};
