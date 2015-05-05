using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace scrotie
{
    
    public partial class SEedit : Form
    {
        XmlRead DataC;
        public SEedit()
        {
            InitializeComponent();
            
            DataC = new XmlRead("SANDBOX_0_0_0_.sbs");
            DataC.ShowSector();
            dataGridView1.DataSource = DataC.Ships;
            label1.Text = "Objects: " + DataC.Ships.Rows.Count.ToString();
        }

        private void QuitButtonClick(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void RemoveButtonClick(object source, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                if (dataGridView1.SelectedRows[0].Cells[0].Value != null)
                {
                    var Result = MessageBox.Show("Delete " + dataGridView1.SelectedRows[0].Cells[1].Value.ToString() + "?",
                        "Confirm Delete!", MessageBoxButtons.YesNo);
                    if (Result == DialogResult.Yes)
                    {
                        int count = 0;
                        foreach (DataGridViewRow Row in dataGridView1.SelectedRows)
                        {
                            DataC.RemoveObject(Row.Cells[0].Value.ToString());
                            var Squery = DataC.Ships.AsEnumerable().Where(r => r.Field<string>("ID") == Row.Cells[0].Value.ToString());
                            var row = Squery.ToList();
                            row[0].Delete();
                            count++;
                        }
                        MessageBox.Show(count + "Object(s) Removed");
                        dataGridView1.DataSource = DataC.Ships;
                        label1.Text = "Objects: " + DataC.Ships.Rows.Count.ToString();
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a Row");
            }
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int Row = dataGridView1.Rows.Count - 1;
            try { Row = dataGridView1.SelectedCells[0].RowIndex; }
            catch { Row--; }
            if (Row >= 0)
            {
                dataGridView1.Rows[Row].Selected = true;
                if (dataGridView1.SelectedRows[0].Cells[0].Value != null)
                {
                    DataC.ShowObject(dataGridView1.SelectedRows[0].Cells[0].Value.ToString());
                    dataGridView2.DataSource = DataC.Components;
                    label2.Text = "Components: " + DataC.Components.Rows.Count.ToString();
                }
            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.F)
            {
                FindDialog FindString = new FindDialog();
                if (FindString.ShowDialog() == DialogResult.OK)
                {
                    string FString = FindString.textBox1.Text;
                    DataGridViewRow row = null;
                    try
                    {
                        row = dataGridView1.Rows
                            .Cast<DataGridViewRow>()
                            .Where(r => r.Cells[0].Value.ToString().Contains(FString)).First();
                    }
                    catch { }
                    if (row == null)
                    {
                        try
                        {
                            row = dataGridView1.Rows
                                .Cast<DataGridViewRow>()
                                .Where(r => r.Cells[1].Value.ToString().Contains(FString)).First();
                        }
                        catch { }
                    }

                    if (row != null)
                    {
                        dataGridView1.CurrentCell = dataGridView1[0, row.Index];
                        dataGridView1.Rows[row.Index].Selected = true;
                    }
                    else
                    {
                        MessageBox.Show(FString + " not Found");
                    }
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int Row = dataGridView1.Rows.Count - 1;
            try { Row = dataGridView1.SelectedCells[0].RowIndex; }
            catch { Row--; }
            if (Row >= 0)
            {
                dataGridView1.Rows[Row].Selected = true;
                if (dataGridView1.SelectedRows[0].Cells[0].Value != null)
                {
                    DataC.ShowObject(dataGridView1.SelectedRows[0].Cells[0].Value.ToString());
                    dataGridView2.DataSource = DataC.Components;
                    label2.Text = "Components: " + DataC.Components.Rows.Count.ToString();
                }
            }
        }

    }
    public class XmlRead
    {
        string FileName = "SANDBOX_0_0_0_.sbs";
        XDocument xmlDoc;
        static DataTable ComponentTable = new DataTable();
        static DataTable ShipTable = new DataTable();
        
        public XmlRead(string File)
        {
            if (File.Length > 0) FileName = File;
            try { xmlDoc = XDocument.Load(FileName); }
            catch
            {
                var SelectFile = new OpenFileDialog();
                if (SelectFile.ShowDialog() == DialogResult.OK)
                {
                    FileName = SelectFile.FileName;
                    try { xmlDoc = XDocument.Load(FileName); }
                    catch { MessageBox.Show("Unable to open " + FileName); Environment.Exit(1); }
                }
                else
                {
                    MessageBox.Show("Unable to open " + FileName);
                    Environment.Exit(1);
                }
            }
        }
        public string truncate(string input, char letter)
        {
            int Index = input.IndexOf(letter);
            if (Index != -1) return input.Substring(0, Index);
            else return input;
        }
        public void ShowObject(string objID)
        {
            if (ComponentTable.Columns.Count == 0)
            {
                ComponentTable.Columns.Add("Type", typeof(string));
                ComponentTable.Columns.Add("SubtypeName", typeof(string));
                ComponentTable.Columns.Add("CustomName", typeof(string));
            }
            ComponentTable.Clear();
            List<XElement> items = (from el in xmlDoc.Descendants("MyObjectBuilder_EntityBase")
                                    where
                                    (
                                        (string)el.Element("EntityId") == objID
                                    )
                                    select el).ToList<XElement>();
            XElement Comp = items[0];
            if (Comp.Element("Item") != null) //items
            {
                ComponentTable.Columns.RemoveAt(2);
                ComponentTable.Columns.Add("Amount", typeof(string));
                string TypeS = Comp.Element("Item").Element("PhysicalContent").FirstAttribute.Value;
                ComponentTable.Rows.Add(TypeS, Comp.Element("Item").Element("PhysicalContent").Element("SubtypeName").Value, Comp.Element("Item").Element("Amount").Value);
            }
            else if (Comp.Element("StorageName") != null) //asteroid
            {
            }
            else
            {//base/ship
                IEnumerable<XElement> Blocks = Comp.Descendants("MyObjectBuilder_CubeBlock");
                ComponentTable.Columns.RemoveAt(2);
                ComponentTable.Columns.Add("CustomName", typeof(string));
                foreach (XElement el in Blocks)
                {
                    string TypeS = "", SubtypeName = "" , CustomName = "";
                    TypeS = el.FirstAttribute.Value.ToString();
                    if (el.Element("SubtypeName") != null) SubtypeName = el.Element("SubtypeName").Value.ToString();
                    if (el.Element("CustomName") != null) CustomName = el.Element("CustomName").Value.ToString();
                    ComponentTable.Rows.Add(TypeS, SubtypeName, CustomName);
                }
            }

        }
        public void ShowSector()
        {
            ShipTable.Columns.Add("ID", typeof(string));
            ShipTable.Columns.Add("Ship", typeof(string));
            ShipTable.Columns.Add("X", typeof(string));
            ShipTable.Columns.Add("Y", typeof(string));
            ShipTable.Columns.Add("Z", typeof(string));

            IEnumerable<XElement> items = xmlDoc.Descendants("MyObjectBuilder_EntityBase");
            foreach (XElement e in items)
            {
                if (e.Element("DisplayName") == null)//not a ship
                {
                    if (e.Element("StorageName") == null) //not an asteroid
                    {
                        if (e.Element("Item") == null)//not an item
                        {
                            //Console.WriteLine(e);//unknown..still a mystery, so do nothing.
                        }
                        else
                        {
                            ShipTable.Rows.Add(e.Element("EntityId").Value,
                                e.Element("Item").Element("PhysicalContent").Element("SubtypeName").Value,
                                truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("x").Value, '.'),
                                truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("y").Value, '.'),
                                truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("z").Value, '.'));
                        }

                    }
                    else //do nothing to asteroids
                    {
                        /*ShipTable.Rows.Add(e.Element("EntityId").Value,
                        e.Element("StorageName").Value,
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("x").Value,'.'),
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("y").Value,'.'),
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("z").Value,'.'));*/
                    }
                }
                else
                {
                    ShipTable.Rows.Add(e.Element("EntityId").Value,
                                        e.Element("DisplayName").Value,
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("x").Value, '.'),
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("y").Value, '.'),
                        truncate(e.Element("PositionAndOrientation").Element("Position").Attribute("z").Value, '.'));
                }
            }
        }
        public DataTable Ships
        {
            get
            {
                return ShipTable;
            }
        }
        public DataTable Components
        {
            get
            {
                return ComponentTable;
            }
        }
        public int Count
        {
            get
            {
                return ShipTable.Rows.Count;
            }
        }
        public void RemoveObject(string objID)
        {
            xmlDoc.Descendants("MyObjectBuilder_EntityBase").Where(xe => xe.Element("EntityId").Value == objID).Single().Remove();
            xmlDoc.Save(FileName);
        }
    }
}
