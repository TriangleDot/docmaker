Welcome to Docmaker!
===================

Docmaker is a program for *manually* making code documentation or websites using **markdown**
Also, for your friends who refuse to download docmaker - It produces *almost-pure* Markdown that can be used without docmaker itself.

Why docmaker
---------------------
Because it's your only option.
Docmaker is the **only graphical program** for making manual documentation that I know of. (I researched a lot)
Unlike other similar note-taking programs, docmaker is specifically designed for code documentation. (It can also be used for note-taking)

#### Features:
- Tree-based node system for organizing your documentation
- Special nodes for Modules, Classes, and Functions. This lets you have functions seperate from your class, but still compiled into one file.
- HTML export with easily customizable templates
- Real directory tree that's easy to use without docmaker
- Tree search in HTML and the editor

Installation
------------
Soon, there will be downloadable executables from <https://triangledot.org>
For now, check out the Building Guide

Building Guide
---------------
### Linux:
1. Download the repo
2. Run `dotnet build`
3. Run `dotnet publish -c Release --self-contained -r ubuntu.18.04-x64`

### Windows:
Your guess is as good as mine!
It needs GTK#, which is difficult to install, and even harder to compile into one .exe

Special thanks
--------------
This project was build with **.NET Core** and **GTK#**.
It used **CommonMark.NET** for markdown compilation.
And **toml-net** for reading the `template.toml` file.

The default theme also uses **flayout** for layout positioning, and **highlight.js** for syntax highlighting.


**This was made by Triangledot**