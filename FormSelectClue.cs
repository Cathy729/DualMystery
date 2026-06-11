using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DualMystery
{
    public class FormSelectClue : Form
    {
        public Clue SelectedClue { get; private set; }
        private ComboBox cmbClues;

        public FormSelectClue(List<Clue> clues)
        {
            this.Text = "分享线索";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;

            Label lbl = new Label
            {
                Text = "请选择你要分享的线索：",
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.GetFont(9f)
            };

            cmbClues = new ComboBox
            {
                Location = new Point(20, 40),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                DataSource = clues
            };

            Button btnOk = new Button
            {
                Text = "确定",
                Location = new Point(80, 75),
                DialogResult = DialogResult.OK,
                Font = Theme.GetFont(9f)
            };
            Theme.StyleButton(btnOk);
            btnOk.Click += (s, e) => { SelectedClue = cmbClues.SelectedItem as Clue; };

            Button btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(170, 75),
                DialogResult = DialogResult.Cancel,
                Font = Theme.GetFont(9f)
            };
            Theme.StyleButton(btnCancel);

            this.Controls.Add(lbl);
            this.Controls.Add(cmbClues);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
