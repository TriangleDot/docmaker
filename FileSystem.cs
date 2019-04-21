using System;
using System.IO;
using Tree;
using Gtk;
using Log;

using System.Collections.Generic;


namespace FileSystem
{
    class FileSystem {
        public String path;
        private FileTree tree;
        private Logger log;
        private Editor editor;
        private bool active = true;
        public FileSystem(String _path, FileTree _tree = null, Editor _editor = null) {
            log = new Logger("FileSystem");
            path = _path;
            if (_tree == null) { // Inactive mode - the FileSystem isn't projecting to a TreeView
                active = false;
            }
            else {
                active = true;
                editor = _editor;
            }
            
            

            
            tree = _tree;
            if (active) {
                tree.callback = onItemClicked;
            }
            scanPath("root",path);
        }
        
        private void scanPath(String place,String real_path) {
            log.info($"Looking at directory: {place}");
            IEnumerable<String> dirs;
            try {
                dirs = Directory.EnumerateDirectories(real_path);
            }
            catch {
                log.error($"Cannot load path {real_path}");
                return;
            }
            foreach (String dir in dirs) { // Look for directories
                String real_dir = dir.Split(Path.DirectorySeparatorChar)[dir.Split(Path.DirectorySeparatorChar).Length-1];
                log.info($"Found {dir}");
                

                if (real_dir.StartsWith("mod_")) {
                    tree.addNode(place,real_dir.Replace("mod_",""),IndexTypes.Module,real_path+Path.DirectorySeparatorChar+real_dir+Path.DirectorySeparatorChar+real_dir+".mod.md");
                    scanPath(place+"/"+real_dir.Replace("mod_",""),real_path+Path.DirectorySeparatorChar+real_dir);
                    loadFunctions(place+"/"+real_dir.Replace("mod_",""),real_path+Path.DirectorySeparatorChar+real_dir+Path.DirectorySeparatorChar+real_dir+".mod.md");
                }
                else {
                    
                    tree.addNode(place,real_dir,IndexTypes.Folder,real_path+Path.DirectorySeparatorChar+real_dir);
                    
                    scanPath(place+"/"+real_dir,real_path+Path.DirectorySeparatorChar+real_dir);
                }

            }
            foreach (String page in Directory.EnumerateFiles(real_path,"*.md")) { // Look for pages
                String file = page.Split(Path.DirectorySeparatorChar)[page.Split(Path.DirectorySeparatorChar).Length-1];
                log.info($"Found File {file}");
                
                if (file.EndsWith(".mod.md")) {
                    // Ignore - It has already been processed above
                }
                else if (file.EndsWith(".class.md")) {
                    log.info($"Loading class at path {real_path+Path.DirectorySeparatorChar+file}");
                    
                    tree.addNode(place,file.Replace(".class.md",""),IndexTypes.Class,real_path+Path.DirectorySeparatorChar+file); // Add to tree
                    
                    loadFunctions(place+"/"+file.Replace(".class.md",""),real_path+Path.DirectorySeparatorChar+file);
                }
                else {
                    tree.addNode(place,file.Replace(".md",""),IndexTypes.Page, real_path+Path.DirectorySeparatorChar+file); // Add to tree
                }
            }
        }

        private void loadFunctions(String place, String real_path) {
            FuncParser parser = new FuncParser(real_path);
            foreach (String name in parser.getFunctions().Keys) {
                log.info("Loading function at "+real_path);
                
                tree.addNode(place,name,IndexTypes.Function, real_path);
                
            }
        }

        bool onItemClicked(TreeIter iter) {

            (bool,TreeIndex) result = tree.getIndexFromIter(iter,"root");
            TreeIndex item = result.Item2;
            log.info($"File path: {item.path}");
            editor.openFile(item.path, item.label, item.type);

            return true;
        }

        public bool insertPage(TreeIndex index, String name) {
            if (name == "") {
                return false;
            }
            if (index.type == IndexTypes.Module || index.type == IndexTypes.Folder) {
                String bpath;
                if (index.type == IndexTypes.Module) {
                    bpath = Path.GetDirectoryName(index.path);
                }
                else {
                    bpath = index.path;
                }
                File.WriteAllText(bpath+Path.DirectorySeparatorChar+name+".md","");
                tree.addNode(index.tree_path,name,IndexTypes.Page,bpath+Path.DirectorySeparatorChar+name+".md");
                editor.openFile(bpath+Path.DirectorySeparatorChar+name+".md",name,IndexTypes.Page);
                return false;
            }
            else {
                return true;
            }
        }

        public bool insertClass(TreeIndex index, String name) {
            if (name == "") {
                return false;
            }
            if (index.type == IndexTypes.Module || index.type == IndexTypes.Folder) {
                String bpath;
                if (index.type == IndexTypes.Module) {
                    bpath = Path.GetDirectoryName(index.path);
                }
                else {
                    bpath = index.path;
                }
                File.WriteAllText(bpath+Path.DirectorySeparatorChar+name+".class.md",$@"
## Class **{name}**
");
                tree.addNode(index.tree_path,name,IndexTypes.Class,bpath+Path.DirectorySeparatorChar+name+".class.md");
                editor.openFile(bpath+Path.DirectorySeparatorChar+name+".class.md",name,IndexTypes.Class);
                return false;
            }
            else {
                return true;
            }
        }

        public bool insertFolder(TreeIndex index, String name) {
            if (name == "") {
                return false;
            }
            if (index.type == IndexTypes.Module || index.type == IndexTypes.Folder) {
                String bpath;
                if (index.type == IndexTypes.Module) {
                    bpath = Path.GetDirectoryName(index.path);
                }
                else {
                    bpath = index.path;
                }
                Directory.CreateDirectory(bpath+Path.DirectorySeparatorChar+name);
                tree.addNode(index.tree_path,name,IndexTypes.Folder,bpath+Path.DirectorySeparatorChar+name);
                //editor.openFile(index.path,name,IndexTypes.Function);
                return false;
            }
            else {
                return true;
            }
        }

        public bool insertModule(TreeIndex index, String name) {
            if (name == "") {
                return false;
            }
            if (index.type == IndexTypes.Module || index.type == IndexTypes.Folder) {
                String bpath;
                if (index.type == IndexTypes.Module) {
                    bpath = Path.GetDirectoryName(index.path);
                }
                else {
                    bpath = index.path;
                }
                Directory.CreateDirectory(bpath+Path.DirectorySeparatorChar+"mod_"+name);
                File.WriteAllText(bpath+Path.DirectorySeparatorChar+"mod_"+name+Path.DirectorySeparatorChar+"mod_"+name+".mod.md",$@"
# Module **{name}**
");
                tree.addNode(index.tree_path,name,IndexTypes.Module,bpath+Path.DirectorySeparatorChar+"mod_"+name+Path.DirectorySeparatorChar+"mod_"+name+".mod.md");
                editor.openFile(bpath+Path.DirectorySeparatorChar+"mod_"+name+Path.DirectorySeparatorChar+"mod_"+name+".mod.md",name,IndexTypes.Module);
                return false;
            }
            else {
                return true;
            }
        }

        public bool insertFunction(TreeIndex index, String name) {
            if (name == "") {
                return false;
            }
            if (index.type != IndexTypes.Page && index.type != IndexTypes.Folder) {
                String bpath;
                if (index.type == IndexTypes.Module) {
                    bpath = Path.GetDirectoryName(index.path);
                }
                else {
                    bpath = index.path;
                }
                FuncParser parser = new FuncParser(index.path);
                parser[name] = $@"
### Function **{name}**()

";
                parser.reSave();
                
                tree.addNode(index.tree_path,name,IndexTypes.Function,index.path);
                editor.openFile(index.path,name,IndexTypes.Function);
                return false;
            }
            else {
                return true;
            }
        }

        public bool deleteNode(TreeIndex index, String name) {

            editor.closeFile();

            switch (index.type) {
                case IndexTypes.Class: {
                    File.Delete(index.path);
                    
                    tree.removeNode(index);
                    break;
                }
                case IndexTypes.Page: {
                    File.Delete(index.path);
                    tree.removeNode(index);
                    break;
                }
                case IndexTypes.Module: {
                    String bpath;
                    if (index.type == IndexTypes.Module) {
                        bpath = Path.GetDirectoryName(index.path);
                    }
                    else {
                        bpath = index.path;
                    }
                    System.IO.DirectoryInfo di = new DirectoryInfo(bpath);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete(); 
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true); 
                    }
                    di.Delete();
                    tree.removeNode(index);

                    break;
                }
                case IndexTypes.Folder: {
                    System.IO.DirectoryInfo di = new DirectoryInfo(index.path);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete(); 
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true); 
                    }
                    di.Delete();
                    tree.removeNode(index);

                    break;
                }
                case IndexTypes.Function: {
                    FuncParser parser = new FuncParser(index.path);
                    parser.functions.Remove(index.label);
                    parser.reSave();
                    tree.removeNode(index);
                    break;
                }
            }
            return false;
        }

        
    }

    class FuncParser {

        private String filename;
        private String _head;
        public Dictionary<String,String> functions;
        private Logger log;
        public FuncParser(String _filename) {
            log = new Logger("FuncParser");
            filename = _filename;
            functions = new Dictionary<String,String>();
            parse();
        }
        
        public void parse() {
            functions = new Dictionary<String,String>();
            String text = System.IO.File.ReadAllText(filename);
            List<String> bits = new List<String>(text.Split("@##@"));
            _head = bits[0];
            bits.RemoveAt(0);
            foreach (String func in bits) {
                String[] name_chunks = func.Split("(!):");
                if (name_chunks.Length < 2) {
                    log.error($"Can't load function {name_chunks[0]} in file {filename}");
                    continue;
                }
                log.info(name_chunks.ToString());
                functions[name_chunks[0]] = name_chunks[1];
            }
        }

        public void reSave() {
            String output = head;
            //output += Environment.NewLine;
            foreach(String i in functions.Keys) {
                output += $"@##@{i}(!):";
                output += functions[i];
                log.info($"Adding function {i} to {filename}");
            }
            using (System.IO.StreamWriter file = 
                    new System.IO.StreamWriter(filename, true))
            {
                file.Write(output);
                file.Flush();
            }
            File.WriteAllText(filename,output);
            log.info($"Saved file {filename}!");
            

        }

        public Dictionary<String,String> getFunctions() {
            return functions;
        }

        public String getHead() {
            return _head;
        }

        public String this[string index]
        {
            get
            {
                // get the item for that index.
                return functions[index];
            }
            set
            {
                functions[index] = value;
            }
            
        }

        public String head {
            get {
                return _head;
            }
            set {
                _head = value;
            }
        }

    }
}