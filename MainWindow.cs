using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using sio = System.IO;

// My libraries
using Log;
using Tree;
using FileSystem;
using Export;
using NonJankExporter;
using System.Collections.Generic;

namespace docmaker
{
    class MainWindow : Window
    {
        [UI] private Label _label1 = null;
        
        private FileTree tree = null;

        private int _counter;

        private Logger log = new Logger("MainWindow");

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private FileSystem.FileSystem files;
        
        private Editor editor;

        String configDir;
        String configFile;

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            tree = new FileTree(true, (TreeView)builder.GetObject("tre"),(Entry)builder.GetObject("searc"));
            editor = new Editor((TextView)builder.GetObject("editor"));
            
            //TreeView treewid = (TreeView)builder.GetObject("tree");
            //treewid.SearchEntry = (Entry)builder.GetObject("search");
            
            
            DeleteEvent += Window_DeleteEvent;
            MenuItem insert_page = (MenuItem)builder.GetObject("menuInsertPage");
            insert_page.Activated += insertPage;

            MenuItem insert_folder = (MenuItem)builder.GetObject("menuInsertFolder");
            insert_folder.Activated += insertFolder;

            MenuItem insert_module = (MenuItem)builder.GetObject("menuInsertModule");
            insert_module.Activated += insertModule;

            MenuItem insert_class = (MenuItem)builder.GetObject("menuInsertClass");
            insert_class.Activated += insertClass;

            MenuItem insert_function = (MenuItem)builder.GetObject("menuInsertFunction");
            insert_function.Activated += insertFunction;

            MenuItem delete = (MenuItem)builder.GetObject("delete");
            delete.Activated += deleteItem;

            MenuItem file_save = (MenuItem)builder.GetObject("fileSave");
            file_save.Activated += save;

            MenuItem file_open = (MenuItem)builder.GetObject("open");
            file_open.Activated += openDialog;

            MenuItem file_export = (MenuItem)builder.GetObject("export");
            file_export.Activated += exportHtml;

            //log.info(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+sio.Path.DirectorySeparatorChar+"docmaker";
            configFile = configDir+sio.Path.DirectorySeparatorChar+"lastUsedPath";
            if (!sio.Directory.Exists(configDir)) {
                sio.Directory.CreateDirectory(configDir);
                sio.Directory.CreateDirectory(configDir+sio.Path.DirectorySeparatorChar+"templates");
                sio.Directory.Move(AppDomain.CurrentDomain.BaseDirectory+"default",configDir+sio.Path.DirectorySeparatorChar+"templates"+sio.Path.DirectorySeparatorChar+"default");
                sio.File.WriteAllText(configFile,"wazzah!!!");
            }
            String file_data = sio.File.ReadAllText(configFile);
            if (sio.Directory.Exists(file_data)) {
                openDir(file_data);
            }
            

            //openDir("docs");
            
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void openDir(String dir) {
            tree.flush();
            files = new FileSystem.FileSystem(dir,tree,editor);
            tree.getIndexAtPath("root").path = dir;
            sio.File.WriteAllText(configFile,dir);
        }

        void openDialog(object sender, EventArgs a) {
            Gtk.FileChooserDialog fc=
                new Gtk.FileChooserDialog("Choose docs folder",
                    this, 
                    Gtk.FileChooserAction.SelectFolder,
                    "Cancel",Gtk.ResponseType.Cancel,
                    "Open",Gtk.ResponseType.Accept);

            if (fc.Run() == (int)Gtk.ResponseType.Accept) 
            {
                openDir(fc.Filename);

            }
            //Destroy() to close the File Dialog
            fc.Destroy();
        }

        private String entryDialog(String body, String defau) {
            MessageDialog d = new MessageDialog(this,DialogFlags.Modal,MessageType.Question,ButtonsType.OkCancel, body);
            Entry entry = new Entry();
            entry.Buffer.Text = defau;
            entry.Show();
            d.ContentArea.PackEnd(entry,true,true,10);
            //d.Activate += _ => d.Respond(ResponseType.Ok);
            int r = d.Run();
            String text = entry.Buffer.Text;
            d.Destroy();
            if (r == (int)ResponseType.Ok) {
                return text;
            }
            else {
                return "";
            }
        }

        private String comboDialog(List<string> items) {
            MessageDialog d = new MessageDialog(this,DialogFlags.Modal,MessageType.Question,ButtonsType.OkCancel, "Select a Template:");
            
            var combo = new ComboBox(items.ToArray());
            combo.Show();
            d.ContentArea.PackEnd(combo,true,true,10);
            //d.Activate += _ => d.Respond(ResponseType.Ok);
            int r = d.Run();
            String text = items[combo.Active];
            d.Destroy();
            if (r == (int)ResponseType.Ok) {
                return text;
            }
            else {
                return "";
            }
        }

        void insertPage(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                err = files.insertPage(item,entryDialog("Name for new item: ","UntitledPage"));
            }
            catch {
                log.error("Error caught while adding new item. Probably caused by there being no selection. (This happening is impossible, so if you see this message, you've glitched the system way more than I thought possible)");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Cannot add new item. Are you sure you selected a correct parent from the tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Page inserted!");
        }

        void insertFolder(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                err = files.insertFolder(item,entryDialog("Name for new item: ","UntitledFolder"));
            }
            catch {
                log.error("Error caught while adding new item. Probably caused by there being no selection. (This happening is impossible, so if you see this message, you've glitched the system way more than I thought possible)");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Cannot add new item. Are you sure you selected a correct parent from the tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Page inserted!");
        }

        void insertClass(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                err = files.insertClass(item,entryDialog("Name for new item: ","UntitledClass"));
            }
            catch {
                log.error("Error caught while adding new item. Probably caused by there being no selection. (This happening is impossible, so if you see this message, you've glitched the system way more than I thought possible)");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Cannot add new item. Are you sure you selected a correct parent from the tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Page inserted!");
        }

        void insertModule(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                err = files.insertModule(item,entryDialog("Name for new item: ","UntitledModule"));
            }
            catch {
                log.error("Error caught while adding new item");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Cannot add new item. Are you sure you selected a correct parent from the tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Page inserted!");
        }

        void insertFunction(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                err = files.insertFunction(item,entryDialog("Name for new item: ","UntitledFunction"));
            }
            catch {
                log.error("Error caught while adding new item. Probably caused by there being no selection. (This happening is impossible, so if you see this message, you've glitched the system way more than I thought possible)");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Cannot add new item. Are you sure you selected a correct parent from the tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Page inserted!");
        }

        void deleteItem(object sender, EventArgs e) {
            bool err = false;
            try {
                TreeIndex item = tree.getSelectedItem();
                MessageDialog d = new MessageDialog(this,DialogFlags.Modal,MessageType.Question,ButtonsType.OkCancel, $"Are you sure you want to delete item '{item.label}'");
                //err = files.insertFunction(item,entryDialog("Name for new item: ","UntitledFunction"));
                ResponseType r = (ResponseType)d.Run();
                d.Destroy();
                if (r == ResponseType.Ok) {
                    files.deleteNode(item,item.label);
                }
            }
            catch {
                log.error("Error caught while deleting file.");
                err = true;
            }
            if (err == true) {
                MessageDialog md = new MessageDialog(this, 
                    DialogFlags.DestroyWithParent, MessageType.Error, 
                    ButtonsType.Close, "Unknown error deleting the selected item. Are you sure you have the right permissions set for your file tree?");
                md.Run();
                md.Destroy();
            }
            
            log.info("Item deleted");
        }

        void save(object sender, EventArgs e) {
            editor.saveFile();
        }

        void exportHtml(object sender, EventArgs e) {
            Gtk.FileChooserDialog fc=
                new Gtk.FileChooserDialog("Choose docs folder",
                    this, 
                    Gtk.FileChooserAction.SelectFolder,
                    "Cancel",Gtk.ResponseType.Cancel,
                    "Open",Gtk.ResponseType.Accept);

            String dname;
            if (fc.Run() == (int)Gtk.ResponseType.Accept) 
            {
                dname = fc.Filename;
                fc.Destroy();
                Templates templates = new Templates();
                String tmplate = comboDialog(templates.getTemplates());
                if (tmplate != "") {
                    String title = entryDialog("Title of the html documents", "UntitledExport");
                    new NoJankExport(files.path,dname,tmplate, title).export();
                }

            }
            else {
                fc.Destroy();
                return;
            }
            //Destroy() to close the File Dialog
            
            
        }
    }
}
