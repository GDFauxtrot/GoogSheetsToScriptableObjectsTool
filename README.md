# GoogSheetsToScriptableObjectsTool
An editor tool built for Unity that accepts Google Sheets URLs, downloads and converts the contents to usable data structures, ScriptableObjects, in Unity with one button press. Made in a few hours.

The tool currently is structured to create "card data" for a card game, but can be trivially modified for many things inside of the "CardDataImporterWindow" class (the line is labelled with exclamation marks as such).

The goal of this tool was to streamline the development of a card game for a friend. With this tool, they are now able to generate all of the in-game card data without touching Unity at all, and the card data can be modified by the rest of the team in a shared Google Sheets document.

This tool can be improved, of course. It was built in the span of a few hours, but with maybe a few more days of polish at most, it could become a more powerful tool that understands the context of CSV's and parse multiple ScriptableObject types, or perhaps parse other types of files (XML, JSON, you name it) to ScriptableObjects.
