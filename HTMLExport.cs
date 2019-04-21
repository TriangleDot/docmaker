using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FileSystem;
using Gtk;
using Tree;
using Log;
using System.Collections.Generic;

namespace NonJankExporter
{
    class NoJankExport {
        private readonly string inpath;
        private readonly string outpath;
        private readonly string template;
        private readonly string title;
        private String tmp_folder;
        private String tmp_module;
        private String tmp_page;
        private String tmp_class;
        private String tmp_function;
        private FileSystem.FileSystem files;
        private String tmp;
        private Templates templates = new Templates();

        private Tree.FileTree tree;

        private Logger log = new Logger("Exporter (-- jank)");

        public NoJankExport(String inpath, String outpath, String template, String title) {
            this.inpath = inpath;
            this.outpath = outpath;
            this.template = template;
            this.title = title;
            loadTemplate();
            tree = new FileTree(false);

            files = new FileSystem.FileSystem(inpath, tree);


        }

        public void export() {
            recExport(tree.getIndexAtPath("root"));
            makeSearch();
        }

        private void makeSearch() {

            TreeIndex index = tree.getIndexAtPath("root");
            String json = getSearchJson();
            String text = useTemplate(index,templates.getTemplate(template).search_Data.Replace("{{searchJson}}",json));
            File.WriteAllText(outpath+Path.DirectorySeparatorChar+"search.html",text);
        }
        
        private String getSearchJson() {
            return "["+string.Join(",",recSearchJson(tree.getIndexAtPath("root")))+"]";
        }

        private List<String> recSearchJson(TreeIndex index) {
            List<String> l = new List<String>();
            foreach (TreeIndex i in index.content) {
                if (i.type != IndexTypes.Folder) {
                    String link = parseLinks($"[[{i.tree_path}]]",tree.getIndexAtPath("root"));
                    l.Add($"['{i.label}','{link}']");
                    l.AddRange(recSearchJson(i));
                }
            }
            return l;
        }

        private void recExport(TreeIndex folder) {
            switch (folder.type) {
                case IndexTypes.Folder: {
                    try {
                        Directory.CreateDirectory(folder.path.Replace(inpath,outpath));
                        
                    }
                    catch {
                        log.warning("At root directory");
                    }
                    foreach (TreeIndex thing in folder.content) {
                        recExport(thing);
                    }
                    
                    break;
                }
                case IndexTypes.Page: {
                    File.WriteAllText(folder.path.Replace(inpath,outpath)+".html",useTemplate(folder));
                    break;
                }
                case IndexTypes.Class: {
                    File.WriteAllText(folder.path.Replace(inpath,outpath)+".html",useTemplate(folder));
                    break;
                }
                case IndexTypes.Module: {
                    Directory.CreateDirectory(Path.GetDirectoryName(folder.path).Replace(inpath,outpath));
                    File.WriteAllText(folder.path.Replace(inpath,outpath)+".html",useTemplate(folder));
                    foreach (TreeIndex thing in folder.content) {
                        recExport(thing);
                    }
                    break;
                }
            }
        }

        public void loadTemplate() {
            // Regex to get template for sidebar folder
            Regex fre = new Regex(@"<sidebar-folder>([\s\S]*?)<\/sidebar-folder>",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            String tpf = templates.getTemplate(template).file_Data;
            tmp_folder = fre.Match(tpf).Value.Replace("<sidebar-folder>","").Replace("</sidebar-folder>","");
            tpf = tpf.Replace(fre.Match(tpf).Value,"");

            // Regex to get template for sidebar module
            Regex mre = new Regex(@"<sidebar-module>([\s\S]*?)<\/sidebar-module>",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            tmp_module = mre.Match(tpf).Value.Replace("<sidebar-module>","").Replace("</sidebar-module>","");
            tpf = tpf.Replace(mre.Match(tpf).Value,"");

            log.info((tmp_folder == tmp_module).ToString());
            // Regex to get template for sidebar page
            Regex pre = new Regex(@"<sidebar-page>([\s\S]*?)<\/sidebar-page>",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            tmp_page = pre.Match(tpf).Value.Replace("<sidebar-page>","").Replace("</sidebar-page>","");
            tpf = tpf.Replace(pre.Match(tpf).Value,"");

            // Regex to get template for sidebar class
            Regex cre = new Regex(@"<sidebar-class>([\s\S]*?)<\/sidebar-class>",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            tmp_class = cre.Match(tpf).Value.Replace("<sidebar-class>","").Replace("</sidebar-class>","");
            tpf = tpf.Replace(cre.Match(tpf).Value,"");

            // Regex to get template for sidebar function
            Regex fure = new Regex(@"<sidebar-function>([\s\S]*?)<\/sidebar-function>",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            tmp_function = fure.Match(tpf).Value.Replace("<sidebar-function>","").Replace("</sidebar-function>","");
            tpf = tpf.Replace(fure.Match(tpf).Value,"");

            tmp = tpf;

            // Build Template assets
            Directory.CreateDirectory(outpath+Path.DirectorySeparatorChar+"_assets");
            foreach (string i in templates.getTemplate(template).assets.Keys) {
                File.WriteAllBytes(outpath+Path.DirectorySeparatorChar+"_assets"+Path.DirectorySeparatorChar+i,templates.getTemplate(template).assets[i]);
            }
        }
        public String useTemplate(TreeIndex index, String tmplate = null) {
            if (tmplate == null) {
                tmplate = tmp;
            }
            int sss;
            if (index.type == IndexTypes.Module) {
                sss = index.tree_path.Split("/").Length-1;
            }
            else {
                sss = index.tree_path.Split("/").Length-2;
            }
            String backdots = "";
            for (int i = 1; i <= sss; i++)
            {
                backdots += "../";
            }

            String thunk = tmplate.Replace("{{content}}",renderFileFromIndex(index));
            thunk = thunk.Replace("{{sidebar}}",renderTree(index));
            return thunk.Replace("_assets/",backdots+"_assets/").Replace("{{searchpath}}",backdots+"search.html").Replace("{{title}}", title);
        }
        private String parseLinks(String text, TreeIndex index) {
            Regex pbs = new Regex(@"\[\[(.+?)\]\]",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            int sss;
            if (index.type == IndexTypes.Module) {
                sss = index.tree_path.Split("/").Length-1;
            }
            else {
                sss = index.tree_path.Split("/").Length-2;
            }
            String backdots = "";
            for (int i = 1; i <= sss; i++)
            {
                backdots += "../";
            }
            

            MatchCollection matches = pbs.Matches(text);
            foreach (Match match in matches)
            {
                String raw = (String)match.Value.Replace("[[","").Replace("]]","");
                try {
                    TreeIndex real_index = tree.getIndexAtPath(raw);
                    String real_place;
                    if (real_index.type == IndexTypes.Function) {
                        real_place = real_index.parent.path+$".html#{real_index.label}";
                    }
                    else {
                        real_place = real_index.path+".html";
                    }
                    log.info("Parsing link: "+(backdots+(real_place.Replace(inpath,"").Remove(0,1))));

                    text = text.Replace((String)match.Value,(backdots+(real_place.Replace(inpath,"").Remove(0,1))));
                }
                catch {
                    log.warning($"Cannot find link {raw}");
                }
                
            }
            return text;
            
        }

        public String renderTree(TreeIndex index) {
            
            return recRenderTree(tree.getIndexAtPath("root"),index);
        }

        private String recRenderTree(TreeIndex index, TreeIndex target) {
            String output = "";
            foreach (TreeIndex i in index.content) {
                bool active;
                if (i.Equals(tree.getIndexAtPath("root"))) {
                    active = true;
                }
                else {
                    active = getRelations(i,target);
                }
                switch (i.type) {
                    case IndexTypes.Folder: {
                        output += tmp_folder.Replace("{{name}}",i.label).Replace("{{active}}",active.ToString().ToLower())
                                                        .Replace("{{link}}",parseLinks($"[[{i.tree_path}]]",target))
                                                        .Replace("{{tree}}",recRenderTree(i,target));
                        break;
                    }

                    case IndexTypes.Module: {
                        output += tmp_module.Replace("{{name}}",i.label).Replace("{{active}}",active.ToString().ToLower())
                                                        .Replace("{{link}}",parseLinks($"[[{i.tree_path}]]",target))
                                                        .Replace("{{tree}}",recRenderTree(i,target));
                        break;
                    }

                    case IndexTypes.Page: {
                        output += tmp_page.Replace("{{name}}",i.label).Replace("{{active}}",active.ToString().ToLower())
                                                        .Replace("{{link}}",parseLinks($"[[{i.tree_path}]]",target))
                                                        .Replace("{{tree}}",recRenderTree(i,target));
                        break;
                    }

                    case IndexTypes.Class: {
                        output += tmp_class.Replace("{{name}}",i.label).Replace("{{active}}",active.ToString().ToLower())
                                                        .Replace("{{link}}",parseLinks($"[[{i.tree_path}]]",target))
                                                        .Replace("{{tree}}",recRenderTree(i,target));
                        break;
                    }

                    case IndexTypes.Function: {
                        output += tmp_function.Replace("{{name}}",i.label).Replace("{{active}}",active.ToString().ToLower())
                                                        .Replace("{{link}}",parseLinks($"[[{i.parent.tree_path}]]",target))
                                                        .Replace("{{tree}}",recRenderTree(i,target));
                        break;
                    }
                }
                
                
            }
            return output;
        }

        public bool getRelations(TreeIndex index, TreeIndex target) {
            foreach (TreeIndex i in index.content) {
                if (i.Equals(target)) {
                    return true;
                }
            }

            foreach (TreeIndex i in index.content) {
                if (getRelations(i,target) == true) {
                    return true;
                }
            }
            return index.Equals(target);


        }

        public String renderFileFromIndex(TreeIndex index) {
            switch (index.type) {
                case IndexTypes.Page: {
                    String content = File.ReadAllText(index.path);
                    content = parseLinks(content,index);
                    String result = CommonMark.CommonMarkConverter.Convert(content);
                    return result;
                }
                case IndexTypes.Module: {
                    FuncParser parser = new FuncParser(index.path);
                    String result = "";
                    result += CommonMark.CommonMarkConverter.Convert(parseLinks(parser.head,index)); // Add head
                    String classlist = "";
                    foreach (TreeIndex i in index.content) {
                        if (i.type == IndexTypes.Class) {
                            classlist += $"- *class* [**{i.label}**]([[{i.tree_path}]]) ";
                        }
                    }
                    result += CommonMark.CommonMarkConverter.Convert(parseLinks(classlist,index)); // Add classes


                    foreach (TreeIndex i in index.content) {
                        if (i.type == IndexTypes.Function) {
                            result += CommonMark.CommonMarkConverter.Convert(parseLinks(parser.functions[i.label],index));
                        }
                    }

                    return result;

                }

                case IndexTypes.Class: {
                    FuncParser parser = new FuncParser(index.path);
                    String result = "";
                    result += CommonMark.CommonMarkConverter.Convert(parseLinks(parser.head,index)); // Add head

                    foreach (TreeIndex i in index.content) {
                        if (i.type == IndexTypes.Function) {
                            result += CommonMark.CommonMarkConverter.Convert(parseLinks(parser.functions[i.label],index));
                        }
                    }

                    return result;
                }

                case IndexTypes.Function: { // This will never be called by the exporter - just for the in-app display
                    FuncParser parser = new FuncParser(index.path);
                    return CommonMark.CommonMarkConverter.Convert(parseLinks(parser.functions[index.label],index));
                }
            }
            return "";
        }

        
    }

    class TempInfo
    {
        public readonly string file_Data;
        public readonly Dictionary<string, byte[]> assets;
        public readonly string search_Data;

        public TempInfo(String file_data, Dictionary<String,byte[]> assets, String search_data) {
            file_Data = file_data;
            this.assets = assets;
            search_Data = search_data;
        }
    }
    class Templates {
        // Class that loads and manages all the templates

        Dictionary<String,TempInfo> tmps;
        Logger log = new Logger("TemplateLoader");
        public Templates() {
            tmps = new Dictionary<String,TempInfo>();
            String configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+Path.DirectorySeparatorChar+"docmaker"+Path.DirectorySeparatorChar+"templates";
            foreach (String d in Directory.EnumerateDirectories(configDir)) {
                parseDir(d);
            }
            

        }

        public void parseDir(String directory) {
            dynamic config;
            try {
                config = Toml.Toml.Parse(File.ReadAllText(directory+Path.DirectorySeparatorChar+"template.toml"));
            }
            catch {
                log.error("Cannot load template.toml for template "+directory);
                return;
            }
            String mainpath;
            object[] assets;
            String searchpath;
            try {
                mainpath = config.template.main_file;
                assets = config.template.assets;
                searchpath = config.search.search_file;
            }
            catch {
                log.error("Cannot find the right attributes in template.toml");
                return;
            }
            Dictionary<String,byte[]> assets_data = new Dictionary<String,byte[]>();
            foreach (String i in assets) {
                try {
                    assets_data[Path.GetFileName(i)] = File.ReadAllBytes(directory+Path.DirectorySeparatorChar+i);
                }
                catch {
                    log.error($"Asset {i} does not exist!");
                }

            }
            try {
                tmps[Path.GetFileName(directory)] = new TempInfo(File.ReadAllText(directory+Path.DirectorySeparatorChar+mainpath),assets_data,File.ReadAllText(directory+Path.DirectorySeparatorChar+searchpath));
            }
            catch {
                log.error("Cannot load the main or search template files!");
            }

        }

        public TempInfo getTemplate(String name) {
            return tmps[name];
        }

        public List<string> getTemplates() {
            return new List<string>(tmps.Keys);
        }
    }
}