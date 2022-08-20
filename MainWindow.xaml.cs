using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;

namespace DXApplication2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            LoadTables();
            LoadViews();
        }

        private void LoadTables()
        {
            CheckBox cbxAll = new CheckBox();
            cbxAll.Content = "Vše";
            cbxAll.Name = "all";
            cbxAll.Checked += TablesCheckBox_Checked;
            cbxAll.Unchecked += TablesCheckBox_Unchecked;
            stackpanel1.Children.Add(cbxAll);

            Separator separator = new Separator();
            stackpanel1.Children.Add(separator);

            DB db = new DB();
            foreach (string table in db.Tables)
            {
                CheckBox cbx = new CheckBox();
                cbx.Content = table;
                cbx.Name = table;
                stackpanel1.RegisterName(table, cbx);
                stackpanel1.Children.Add(cbx);
            }
        }

        private void LoadViews()
        {
            CheckBox cbxAll = new CheckBox();
            cbxAll.Content = "Vše";
            cbxAll.Name = "all";
            cbxAll.Checked += ViewsCheckBox_Checked;
            cbxAll.Unchecked += ViewsCheckBox_Unchecked;
            stackpanel2.Children.Add(cbxAll);

            Separator separator = new Separator();
            stackpanel2.Children.Add(separator);

            DB db = new DB();
            foreach (string view in db.Views)
            {
                CheckBox cbx = new CheckBox();
                cbx.Content = view;
                cbx.Name = view;
                stackpanel2.RegisterName(view, cbx);
                stackpanel2.Children.Add(cbx);
            }
        }

        private void TablesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetTablesCheckBoxCheckedStatus(true);
        }

        private void TablesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetTablesCheckBoxCheckedStatus(false);
        }

        private void SetTablesCheckBoxCheckedStatus(bool isChecked)
        {
            DB db = new DB();
            foreach (string table in db.Tables)
            {
                object cbx = stackpanel1.FindName(table);
                if (cbx is CheckBox)
                {
                    CheckBox chbx = cbx as CheckBox;
                    chbx.IsChecked = isChecked;
                }
            }
        }

        private void ViewsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetViewsCheckBoxCheckedStatus(true);
        }

        private void ViewsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetViewsCheckBoxCheckedStatus(false);
        }

        private void SetViewsCheckBoxCheckedStatus(bool isChecked)
        {
            DB db = new DB();
            foreach (string view in db.Views)
            {
                object cbx = stackpanel2.FindName(view);
                if (cbx is CheckBox)
                {
                    CheckBox chbx = cbx as CheckBox;
                    chbx.IsChecked = isChecked;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool isCheckedSome = false;
            DB db = new DB();

            foreach (CheckBox cbx in stackpanel1.Children.OfType<CheckBox>())
            {
                if (cbx.IsChecked == true)
                {
                    isCheckedSome = true;

                    if (cbx.Name != "all")
                    {
                        ObjectGenerator generator = new ObjectGenerator(cbx.Name, db);
                        generator.GenerateCSharpCode();

                        string output = "Tabulka " + cbx.Name + " byla vygenerována do souboru c" + generator.ClassName + ".cs";
                        Paragraph p = new Paragraph(new Run(output));
                        richTextBox.Document.Blocks.Add(p);
                    }
                }
            }

            if (!isCheckedSome)
                MessageBox.Show("Nebyla vybrána žádna tabulka ani pohled.");
            else
            {
                richTextBox.AppendText(Environment.NewLine);
                richTextBox.AppendText("-----------------------------------------------------------------------------------------------------");
            }
                
        }
    }
}
