import os
import json
import customtkinter as ctk
from tkinter import filedialog, messagebox

CONFIG_PATH = "config.json"
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))


class LocaleSyncApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Locale Sync Tool")
        self.geometry("800x600")
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")

        self.mod_path = ""
        self.items_dir = ""
        self.quests_dir = ""
        self.locales_dir = ""

        self.load_config()
        self.create_widgets()

    def create_widgets(self):
        self.mod_button = ctk.CTkButton(self, text="Select Mod Directory", command=self.select_mod_dir)
        self.mod_button.pack(pady=10)

        self.sync_button = ctk.CTkButton(self, text="Start Sync", command=self.sync_locales)
        self.sync_button.pack(pady=10)

        self.logger = ctk.CTkTextbox(self, wrap="word", height=400)
        self.logger.pack(fill="both", expand=True, padx=10, pady=10)
        self.logger.insert("end", "Logger initialized...\n")

    def log(self, msg):
        self.logger.insert("end", msg + "\n")
        self.logger.see("end")
        self.update()

    def select_mod_dir(self):
        self.mod_path = filedialog.askdirectory(title="Select Mod Directory")
        if self.mod_path:
            self.items_dir = os.path.join(self.mod_path, "Resources", "db", "CustomItems")
            self.quests_dir = os.path.join(self.mod_path, "Resources", "db", "CustomQuests")
            self.locales_dir = os.path.join(self.mod_path, "Resources", "db", "CustomLocales")
            os.makedirs(self.locales_dir, exist_ok=True)

            self.log(f"Selected Mod Directory: {self.mod_path}")
            self.save_config()

    def load_config(self):
        if os.path.exists(CONFIG_PATH):
            with open(CONFIG_PATH, "r") as f:
                cfg = json.load(f)
                self.mod_path = os.path.abspath(os.path.join(SCRIPT_DIR, cfg.get("mod_path", "")))
                if self.mod_path:
                    self.items_dir = os.path.join(self.mod_path, "Resources", "db", "CustomItems")
                    self.quests_dir = os.path.join(self.mod_path,"Resources", "db", "CustomQuests")
                    self.locales_dir = os.path.join(self.mod_path,"Resources", "db", "CustomLocales")

    def save_config(self):
        mod_rel = os.path.relpath(self.mod_path, SCRIPT_DIR)
        with open(CONFIG_PATH, "w") as f:
            json.dump({"mod_path": mod_rel}, f, indent=2)

    def load_json(self, path):
        try:
            with open(path, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception as e:
            self.log(f"Failed to load {path}: {e}")
            return {}

    def write_json(self, path, data):
        try:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
        except Exception as e:
            self.log(f"Failed to write {path}: {e}")

    def merge_locale_data(self, source_data, dest_path, lang, context_label=""):
        stats = {"created": 0, "updated": 0, "skipped": 0}
        dest_data = self.load_json(dest_path) if os.path.exists(dest_path) else {}
        if not os.path.exists(dest_path):
            self.log(f"Creating new locale file: {dest_path}")

        for key, value in source_data.items():
            if key not in dest_data:
                dest_data[key] = value
                self.log(f"[{lang}] Added {context_label}: {key}")
                stats["created"] += 1
            elif dest_data[key] != value:
                dest_data[key] = value
                self.log(f"[{lang}] Updated {context_label}: {key}")
                stats["updated"] += 1
            else:
                stats["skipped"] += 1

        if stats["created"] or stats["updated"]:
            self.write_json(dest_path, dest_data)

        return stats

    def sync_item_locales(self):
        stats = {"created": 0, "updated": 0, "skipped": 0}

        for filename in os.listdir(self.items_dir):
            if not filename.endswith(".json"):
                continue

            items_data = self.load_json(os.path.join(self.items_dir, filename))
            if not isinstance(items_data, dict):
                continue

            for item_id, item_data in items_data.items():
                locales = item_data.get("locales", {})
                for lang, values in locales.items():
                    locale_path = os.path.join(self.locales_dir, f"{lang}.json")
                    keys = {
                        f"{item_id} Name": values.get("name"),
                        f"{item_id} ShortName": values.get("shortName"),
                        f"{item_id} Description": values.get("description"),
                    }
                    keys = {k: v for k, v in keys.items() if v is not None}
                    lang_stats = self.merge_locale_data(keys, locale_path, lang, "item")
                    for k in stats:
                        stats[k] += lang_stats[k]

        return stats

    def sync_quest_locales(self):
        stats = {"created": 0, "updated": 0, "skipped": 0}
        if not os.path.isdir(self.quests_dir):
            self.log("No Quests directory found.")
            return stats

        for trader_id in os.listdir(self.quests_dir):
            trader_locales_path = os.path.join(self.quests_dir, trader_id, "locales")
            if not os.path.isdir(trader_locales_path):
                continue

            self.log(f"Processing quest locales for trader: {trader_id}")

            for filename in os.listdir(trader_locales_path):
                if not filename.endswith(".json"):
                    continue

                lang = filename.replace(".json", "")
                source_locale_path = os.path.join(trader_locales_path, filename)
                dest_locale_path = os.path.join(self.locales_dir, filename)

                source_data = self.load_json(source_locale_path)
                if not isinstance(source_data, dict):
                    continue

                lang_stats = self.merge_locale_data(source_data, dest_locale_path, lang, "quest")
                for k in stats:
                    stats[k] += lang_stats[k]

        return stats

    def sync_locales(self):
        if not self.items_dir or not self.locales_dir:
            messagebox.showerror("Missing Input", "Please select a mod directory first.")
            return

        item_stats = self.sync_item_locales()
        quest_stats = self.sync_quest_locales()

        self.log("\n--- Sync Complete ---")
        self.log(f"Item Locales - Created: {item_stats['created']}, Updated: {item_stats['updated']}, Skipped: {item_stats['skipped']}")
        self.log(f"Quest Locales - Created: {quest_stats['created']}, Updated: {quest_stats['updated']}, Skipped: {quest_stats['skipped']}")


if __name__ == "__main__":
    app = LocaleSyncApp()
    app.mainloop()
