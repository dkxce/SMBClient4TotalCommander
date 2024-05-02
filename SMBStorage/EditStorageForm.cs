//
// C# 
// TCFsSMBStorage
// v 0.1, 02.05.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMBStorage
{
    public partial class EditStorageForm : Form
    {
        public SMBStorages Storages = null;
        public string OriginalStorageName = String.Empty;
        public string DestinationStorageName = String.Empty;
        public string DestinationLink = String.Empty;
        public string Result = "cancel";

        public void UpdateNames()
        {
            btnDelete.Enabled = Storages.StorageExists(OriginalStorageName);
            tbLink.Text = DestinationLink;
            if (btnDelete.Enabled)
            {
                cbManual.Checked = true;
                tbName.Text = OriginalStorageName;
                Text += $" [{OriginalStorageName}]";
            };
            tbLink_TextChanged(this, null);
        }

        public EditStorageForm()
        {
            InitializeComponent();
        }

        private void EditStorageForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Result = "delete";
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Result = "ok";
            DestinationStorageName = tbName.Text.Trim();
            DestinationLink = tbLink.Text.Trim().Trim(new char[] { '\\' });
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Result = "cancel";
            Close();
        }

        private void tbLink_TextChanged(object sender, EventArgs e)
        {
            string txt = tbLink.Text.Trim().Trim(new char[] { '\\' });
            Regex rx = new Regex(SMBStorages.linkRegex, RegexOptions.IgnoreCase);
            Match mx = rx.Match(txt);
            if (mx.Success)
            {
                tbLink.BackColor = Color.LightGreen;
                if(cbAuto.Checked)
                {
                    SMBStorage si = SMBStorages.FromConnectionString(txt);
                    if (si != null) tbName.Text = si.account;
                    else tbName.Text = "";
                };
            }
            else
            {
                tbLink.BackColor = Color.LightPink;
                if (cbAuto.Checked) tbName.Text = "";
            };
        }

        private void tbName_TextChanged(object sender, EventArgs e)
        {
            string txt = tbName.Text.Trim();
            bool ok = (txt.Length > 0) && (txt.IndexOfAny(Path.GetInvalidPathChars()) == -1);
            if (ok)
            {
                tbName.BackColor = Color.LightGreen;
                btnOk.Enabled = true;
            }
            else
            {
                tbName.BackColor = Color.LightPink;
                btnDelete.Enabled = false;
                btnOk.Enabled = false;
            };
        }

        private void cbManual_CheckedChanged(object sender, EventArgs e)
        {
            tbName.ReadOnly = cbAuto.Checked;
        }

        private void cbAuto_CheckedChanged(object sender, EventArgs e)
        {
            tbName.ReadOnly = cbAuto.Checked;
            tbLink_TextChanged(sender, e);
        }

        private void EditStorageForm_Shown(object sender, EventArgs e)
        {
            tbLink.Focus();
        }
    }
}
