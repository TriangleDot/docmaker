using System;
using System.IO;
using FileSystem;
using Tree;
using Log;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Export
{
    
    class Exporter {
        
        Logger log;
        Template template;
        public Exporter(String inpath, String outpath, String templatepath) {
            log = new Logger("Exporter");
            template = new Template(templatepath,inpath,outpath);
            scanPath("path",inpath);
            template.render("Test");
            
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
                    
                    scanPath(place+"/"+real_dir.Replace("mod_",""),real_path+Path.DirectorySeparatorChar+real_dir);
                    String modpath = real_path+Path.DirectorySeparatorChar+real_dir+Path.DirectorySeparatorChar+real_dir+".mod.md";
                    template.addFolder(real_path,real_dir, place+"/"+real_dir, FileTypes.Module);
                    new MDCompiler(File.ReadAllText(modpath), modpath, place+"/"+real_dir.Replace("mod_","")+modpath.Replace(".mod.md","").Replace("mod_",""), FileTypes.Module, template).compileWithTemplate(template);
                }
                else {
                    
                    scanPath(place+"/"+real_dir,real_path+Path.DirectorySeparatorChar+real_dir);
                    template.addFolder(real_path,real_dir, place+"/"+real_dir, FileTypes.Folder);
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
                    
                    String cpath = real_path+Path.DirectorySeparatorChar+file;
                    new MDCompiler(File.ReadAllText(cpath), cpath, place+"/"+file.Replace(".class.md", ""), FileTypes.Class, template).compileWithTemplate(template);
                }
                else {
                    String ppath = real_path+Path.DirectorySeparatorChar+file;
                    new MDCompiler(File.ReadAllText(ppath), ppath, place+"/"+file.Replace(".md", ""), FileTypes.Class, template).compileWithTemplate(template);
                }
            }
        }
    }

    class MDCompiler {

        String result;
        String path;
        String vpath;
        FileTypes type;

        Logger log = new Logger("MDCompiler");
        Template template;
        public MDCompiler(String text, String _path, String _vpath, FileTypes _type, Template _template) {
            Regex rx = new Regex(@"@##@(.+?)\(!\):",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            Regex pbs = new Regex(@"\[\[(.+?)\]\]",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            template = _template;
            path = _path;
            int parts = path.Replace(template.base_path,"root").Split(Path.DirectorySeparatorChar).Length-2;
            String reppath = "";
            
            for (int i = 0; i < parts; i++)
            {
                reppath += "../";
                
            }
            ///\[(.*?)\]/
            MatchCollection matches = rx.Matches(text);
            foreach (Match match in matches)
            {
                text = text.Replace((String)match.Value,"");
            }

            MatchCollection links = pbs.Matches(text);
            foreach (Match match in links)
            {
                log.info(parsePath((String)match.Value.Replace("[[","").Replace("]]",""),reppath));
                log.info((String)match.Value);
                text = text.Replace((String)match.Value,parsePath((String)match.Value.Replace("[[","").Replace("]]",""),reppath));
            }
            result = CommonMark.CommonMarkConverter.Convert(text.Replace("root/",reppath));
            vpath = _vpath;
            type = _type;
            
        }

        public void compileWithTemplate(Template template) {
            template.addFile(result,path, vpath, type);
        }

        public String parsePath(String path, String reppath) {
            VFile file = template.getVFileFromPath(path);
            return file.path.Replace("root",reppath);
        }
    }

    enum FileTypes
    {
        Folder, Module,
        Class, Page, Function
    }
    class VFolder
    {
        public String name;
        public List<VFolder> folders;
        public List<VFile> file;
        public String path;
        public FileTypes type;
        public String vpath;

        public VFolder(String _name, String _path, String _vpath, FileTypes _type) {
            folders = new List<VFolder>();
            file = new List<VFile>();
            name = _name;
            path = _path;
            vpath = _vpath;
            type = _type;
        }

    }

    class VFile 
    {
        public String name;
        public String content;
        public VFolder parent;
        public String path;
        public FileTypes type;
        public String vpath;
        public VFile(String _name, String _content, VFolder _parent, String _path, String _vpath, FileTypes _type) {
            name = _name;
            content = _content;
            parent = _parent;
            path = _path;
            vpath = _vpath;
            type = _type;
        }
    }
    class Template {

        String tmpdata;
        
        VFolder root;
        public String base_path;
        public String out_path;

        Logger log = new Logger("Template");
        public Template(String path, String inpath, String outpath) {
            tmpdata = File.ReadAllText(path);
            
            base_path = inpath;
            out_path = outpath;
            root = new VFolder("root",out_path,"root",FileTypes.Folder);

        }

        public String addFile(String _htmdata, String path, String vpath, FileTypes type) {
            
            path = path.Replace(base_path,"root");
            String cplate = tmpdata.Replace("{{content}}",_htmdata);
            log.info($"Adding file {path} to directory {Path.GetDirectoryName(path)}");
            VFolder parent = getFolderFromPath(Path.GetDirectoryName(path));
            parent.file.Add(new VFile(Path.GetFileName(path),cplate,parent,path,vpath,type) );
            return cplate;
        }

        public void addFolder(String path, String name, String vpath, FileTypes type) {
            path = path.Replace(base_path,"root");
            VFolder parent = getFolderFromPath(Path.GetDirectoryName(path));
            parent.folders.Add(new VFolder(name,path+Path.DirectorySeparatorChar+name,vpath,type));
        }

        public VFile getFileFromPath(String path) {
            VFolder folder = getFolderFromPath(Path.GetDirectoryName(path));
            foreach (VFile f in folder.file) {
                if (f.name == Path.GetFileName(path)) {
                    return f;
                }
            }
            throw new FileNotFoundException($"Cannot find file {path}");

        }

        public VFolder getFolderFromPath(String path) {
            
            List<String> pathbits = new List<String>(path.Split(Path.DirectorySeparatorChar));
            return getFolderRecursive(pathbits, root);
        }

        private VFolder getFolderRecursive(List<String> pathbits, VFolder searchf) {
            foreach (VFolder i in searchf.folders) {
                if (i.name == pathbits[0]) {
                    pathbits.RemoveAt(0);
                    if (pathbits.Count < 1) {
                        return i;
                    }
                    return getFolderRecursive(pathbits,i);
                }
            }
            throw new FileNotFoundException($"Cannot find folder ");
        }

        public VFile getVFileFromPath(String path) {
            VFolder folder = getFolderFromPath(Path.GetDirectoryName(path));
            foreach (VFile f in folder.file) {
                if (Path.GetFileName(f.vpath) == Path.GetFileName(path)) {
                    return f;
                }
            }
            throw new FileNotFoundException($"Cannot find file {path}");

        }

        public VFolder getVFolderFromPath(String path) {
            
            List<String> pathbits = new List<String>(path.Split("/"));
            return getVFolderRecursive(pathbits, root);
        }

        private VFolder getVFolderRecursive(List<String> pathbits, VFolder searchf) {
            foreach (VFolder i in searchf.folders) {
                if (Path.GetFileName(i.vpath) == pathbits[0]) {
                    pathbits.RemoveAt(0);
                    if (pathbits.Count < 1) {
                        return i;
                    }
                    return getVFolderRecursive(pathbits,i);
                }
            }
            return searchf;
        }

        public String renderfile(String path) {
            return getFileFromPath(path).content;
        }

        public void render(String title) {
            recursiveRender(root,title);
        }


        private void recursiveRender(VFolder bfol, String title) {
            foreach (VFolder f in bfol.folders) {
                log.info($"Scanning folder {f.path}");
                Directory.CreateDirectory(f.path.Replace("root",out_path));
                recursiveRender(f,title);
                
            }
            foreach (VFile d in bfol.file) {
                log.info($"Rendering file {d.path.Replace("root",out_path)+".html"}");
                File.WriteAllText(d.path.Replace("root",out_path)+".html", d.content.Replace("{{title}}",title));
            }
        }
    }
}


