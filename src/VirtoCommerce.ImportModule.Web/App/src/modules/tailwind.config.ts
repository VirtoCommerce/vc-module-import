import defaultConfig from "@vc-shell/framework/tailwind.config";

export default {
  prefix: "tw-",
  content: ["./import/**/*.{vue,js,ts,jsx,tsx}"],
  theme: defaultConfig.theme,
};
