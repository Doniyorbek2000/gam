// Loads the exact bundle Nakama runs (build/index.js) into an isolated context and returns its globals.
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

module.exports = function loadModule() {
    const context = vm.createContext({});
    const source = fs.readFileSync(path.join(__dirname, "..", "build", "index.js"), "utf8");
    vm.runInContext(source, context, { filename: "index.js" });
    return context;
};
