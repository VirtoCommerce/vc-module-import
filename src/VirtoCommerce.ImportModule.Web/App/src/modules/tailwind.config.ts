import defaultConfig from "@vc-shell/framework/tailwind.config";

const config: import("tailwindcss").Config = {
  prefix: "tw-",
  content: ["./**/*.{vue,js,ts,jsx,tsx}"],
  theme: defaultConfig.theme,
};

export default config;
