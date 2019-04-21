using System;
using System.IO;
using Log;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using System.Collections.Generic;
using FileSystem;

namespace Tree {
    class FileTree {
        private TreeView tree;
        private TreeStore fileTree;
        private TreeIndex root; // Root node
        private Logger log;
        private Entry sbar;
        private readonly bool active;
        private TreeModelFilter filter;

        public Func<TreeIter,bool> callback = iter => true; 
        public FileTree(bool active, TreeView _tree = null, Entry _sbar = null ) {
            log = new Logger("FileTree");
            sbar = _sbar;
            this.active = active;
            tree = _tree;
            if (active == false) {
                root = new TreeIndex(IndexTypes.Folder, "root", new List<TreeIndex>(), new TreeIter(), null, "","root");
                return;
            }
            fileTree = new Gtk.TreeStore (typeof (Gdk.Pixbuf), typeof (string));
            tree.RowActivated += callCallback;


            // Create a column for the file names
            
            Gtk.TreeViewColumn pbColumn = new Gtk.TreeViewColumn ();
            pbColumn.Title = "Icon";

            // Create the text cell that will display the artist name
            Gtk.CellRendererPixbuf pbCell = new Gtk.CellRendererPixbuf ();

            // Add the cell to the column
            pbColumn.PackStart (pbCell, true);

            tree.AppendColumn(pbColumn);
            // Create a column for the file names
            
            Gtk.TreeViewColumn fileColumn = new Gtk.TreeViewColumn ();
            fileColumn.Title = "FileName";
            

            // Create the text cell that will display the artist name
            Gtk.CellRendererText filenameCell = new Gtk.CellRendererText ();
            
            

            // Add the cell to the column
            fileColumn.PackStart (filenameCell, true);

            tree.AppendColumn(fileColumn);
            

            fileColumn.AddAttribute (filenameCell, "text", 1);
            pbColumn.AddAttribute (pbCell, "pixbuf", 0);

            
            Gtk.TreeIter iter = fileTree.AppendValues ("root");
            root = new TreeIndex(IndexTypes.Folder, "root", new List<TreeIndex>(), iter, null, "","root");

            

            filter = new Gtk.TreeModelFilter (fileTree, null);

            // Specify the function that determines which rows to filter out and which ones to display

            filter.VisibleFunc = sorter;

            tree.Model = filter;

            sbar.Activated += refilter;
            

        }

        bool sorter (object model, Gtk.TreeIter iter) {
            TreeIndex index = getIndexFromIter(iter,"root").Item2;
            // sbar.Buffer.Text

            return hasStringInIt(index,sbar.Buffer.Text).Item1;
        }

        private (bool,TreeIndex) hasStringInIt(TreeIndex index, String thing) {
            if (index.label.ToLower().Contains(thing.ToLower())) {
                return (true,index);
            }
            foreach (TreeIndex t in index.content) {
                (bool,TreeIndex) a = hasStringInIt(t,thing);
                if (a.Item1 == true) {
                    return a;
                }

            }
            return (false,index);

        }

        void refilter(object a, object b) {
            filter.Refilter();
        }

        public void flush() {
            // Removes the old tree, and replaces it with a brand new one.
            fileTree.Remove(ref root.iter);
            Gdk.Pixbuf icon= new Gdk.Pixbuf(System.IO.File.ReadAllBytes (AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallFolder.png"));
            icon = icon.ScaleSimple(26,26,Gdk.InterpType.Bilinear);
            Gtk.TreeIter iter = fileTree.AppendValues (icon, "root");
            root = new TreeIndex(IndexTypes.Folder, "root", new List<TreeIndex>(), iter, null, "","root");
        }

        

        void callCallback(object sender, RowActivatedArgs e) {
            TreePath path = e.Path;
            TreeIter ity;
            fileTree.GetIter(out ity,path);
            
            callback(ity);
            
        }

        public TreeIndex addNode(String path, String name, IndexTypes type, String filename) {
            String fname = "";
            switch (type) {
                case IndexTypes.Page: {
                    fname = AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallPage.png";
                    break;
                }
                case IndexTypes.Module: {
                    fname = AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallModule.png";
                    break;
                }
                case IndexTypes.Function: {
                    fname = AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallFunction.png";
                    break;
                }
                case IndexTypes.Class: {
                    fname = AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallClass.png";
                    break;
                }
                case IndexTypes.Folder: {
                    fname = AppDomain.CurrentDomain.BaseDirectory+Path.DirectorySeparatorChar+"SmallFolder.png";
                    break;
                }
            }
            log.info($"Adding node at path {filename}");
            TreeIndex parent = getIndexAtPath(path);
            if (!active) {
                TreeIndex ndex = new TreeIndex(type,name,new List<TreeIndex>(),new TreeIter(),parent,filename,path+"/"+name);
                log.info($"Node path from TreeIndex: {ndex.path}");
                parent.content.Add(ndex);
                return ndex;
            }
            Gdk.Pixbuf icon= new Gdk.Pixbuf(System.IO.File.ReadAllBytes (fname));
            icon = icon.ScaleSimple(26,26,Gdk.InterpType.Bilinear);
            TreeIter iter = fileTree.AppendValues(parent.iter, icon, name);
            TreeIndex index = new TreeIndex(type,name,new List<TreeIndex>(),iter,parent,filename,path+"/"+name);
            log.info($"Node path from TreeIndex: {index.path}");
            parent.content.Add(index);
            return index;
        }

        public void removeNode(TreeIndex index)  {
            index.parent.content.Remove(index);
            fileTree.Remove(ref index.iter);
        }
        public TreeIndex this[string index]
        {
            get
            {
                // get the item for that index.
                return getIndexAtPath(index);
            }
            
        }


        public TreeIndex getIndexAtPath(String path) {
            List<String> bits = new List<String>(path.Split("/"));
            bits.RemoveAt(0); // Strip off the root, so we can have a base node to give to the function
            if (bits.Count < 1) { // They're looking for root
                return root;
            }
            return getIndexFromBits(bits,root); // Let recursion do it's magic
            
            
        }

        private TreeIndex getIndexFromBits(List<String> bits, TreeIndex base_node) {
            TreeIndex output = null;
            foreach (TreeIndex i in base_node.content) { // Look through the base_nodes's contents to find a node with the right name
                 if (i.label == bits[0]) {
                     output = i;
                     break;
                 }
            }
            if (output == null) {
                throw new System.IO.DirectoryNotFoundException("Cannot find node at path "+string.Join("/",bits));
            }

            bits.RemoveAt(0);
            if (bits.Count < 1) { // Nothing left in the path
                return output;
            }
            else { // Something left in the path: call this function again
                return getIndexFromBits(bits,output);
            }
        }

        public (bool,TreeIndex) getIndexFromIter(TreeIter iter, String path) {
            foreach (TreeIndex i in getIndexAtPath(path).content) {
                if (i.iter.Equals(iter)) {
                    return (true,i);
                }
                else {
                    if (getIndexFromIter(iter,path+"/"+i.label).Item1) {
                        return getIndexFromIter(iter,path+"/"+i.label);
                    }
                }
            }
            return (false,getIndexAtPath("root"));
        }

        public TreeIndex getSelectedItem() {
            TreeIter iter;
            fileTree.GetIter(out iter,tree.Selection.GetSelectedRows()[0]);

            return getIndexFromIter(iter,"root").Item2;
        }

    }

    enum IndexTypes {Folder, Page, Module, Class, Function};
    class TreeIndex {
        
        public IndexTypes type;
        public String label;
        public List<TreeIndex> content;
        public TreeIter iter;
        public TreeIndex parent;
        public String path;
        public String tree_path;

        public TreeIndex (IndexTypes _type, String _label, List<TreeIndex> _content, TreeIter _iter, TreeIndex _parent, String _path, String _tree_path) {
            type = _type;
            label = _label;
            content = _content;
            iter = _iter;
            parent = _parent;
            path = _path;
            tree_path = _tree_path;

        }

        public override string ToString() {
            
            return "TreeIndex (Type: "+type.ToString()+", Label: "+label+", FileName: "+path+")";
        }

    }

    class Editor {

        private TextView text;
        private String filename;
        private IndexTypes type;
        private String label;
        private FuncParser parser;
        public Editor(TextView editor) {
            text = editor;
            text.Editable = true;

        }

        public String getText() {
            return text.Buffer.Text;
        }
        
        public void setText(String content) {
            text.Buffer.Text = content;
        }

        public void saveFile() {
            
            switch (type) {
                case IndexTypes.Page: 
                {
                    String content = getText();
                    File.WriteAllText(filename, content);
                    break;
                }
                case IndexTypes.Module:
                {
                    parser = new FuncParser(filename);
                    parser.head = getText();
                    parser.reSave();
                    break;
                }
                case IndexTypes.Class:
                {
                    parser = new FuncParser(filename);
                    parser.head = getText();
                    parser.reSave();
                    break;
                }
                case IndexTypes.Function:
                {
                    parser = new FuncParser(filename);
                    parser.functions[label] = getText();
                    parser.reSave();
                    break;
                }
                
            }
        }

        public void closeFile() {
            setText("");
            filename = "";
            type = IndexTypes.Folder;
            label = "";
        }

        public void openFile(String _filename, String _label, IndexTypes _type) {
            saveFile();
            setText("");
            filename = _filename;
            type = _type;
            label = _label;

            switch (type) {
                case IndexTypes.Page: 
                {
                    String content = File.ReadAllText(filename);
                    setText(content);        
                    break;
                }
                case IndexTypes.Module:
                {
                    parser = new FuncParser(filename);
                    setText(parser.head);
                    break;
                }
                case IndexTypes.Class:
                {
                    parser = new FuncParser(filename);
                    setText(parser.head);
                    break;
                }
                case IndexTypes.Function:
                {
                    parser = new FuncParser(filename);
                    setText(parser.functions[label]);
                    break;
                }
                
            }
        }
    }
}