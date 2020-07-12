﻿using Dune2000.Extensions.FileFormats.INI;
using Dune2000.FileFormats.Mis;
using Dune2000.FileFormats.R16;
using Dune2000.Structs.Pal;
using Primrose.Primitives.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dune2000.Editor.UI.Editors.Resources
{
  public partial class ImportDataResourceForm : Form
  {
    public ImportDataResourceForm()
    {
      InitializeComponent();
    }

    public ResourceFile Resource { get { return _resource; } set { if (_resource != value) { _resource = value; } } }
    public PaletteFile Palette { get { return _palette; } set { _palette = value; } }

    private ResourceFile _resource;
    private PaletteFile _palette;

    private async void bOK_Click(object sender, System.EventArgs e)
    {
      Enabled = false;
      bool merge = rbMerge.Checked;
      string rpath = tbFilepath.Text;
      string rootDir;

      ResourceExportINIFile rfile = new ResourceExportINIFile();
      try
      {
        rootDir = Path.GetDirectoryName(rpath);
        rfile.ReadFromFile(rpath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Import failed.\nThe data file is in the incorrect format.\n\nException: {0}".F(ex.Message));
        Enabled = true;
        return;
      }

      try
      {
        if (rfile.EntryCount < _resource.Resources.Count)
        {
          if (MessageBox.Show("The entry count declared in the import data is {0}, but the current resource file has {1} entries.\n" +
                              "If you continue, the resource file will be truncated. Entries above {0:0000} will be cleared.\n" +
                              "Are you sure you want to proceed?".F(rfile.EntryCount, _resource.Resources.Count)
                            , "Dune 2000 Editor"
                            , MessageBoxButtons.YesNo)
                          != DialogResult.Yes)
          {
            return;
          }
        }

        Progress<int> p = new Progress<int>();
        pbarProgress.Maximum = rfile.EntryCount;
        pbarProgress.Value = 0;
        lblProgress.Text = "Setting up...";
        p.ProgressChanged += (s, id) => 
        {
          pbarProgress.Value = id;
          lblProgress.Text = "{0} / {1}".F(id, rfile.EntryCount); 
        };

        IPalette pal = _palette.Palette;
        await Task.Factory.StartNew(() =>
        {
          if (merge)
          {
            rfile.ImportMerge(_resource.Resources, ref pal, rootDir, p);
          }
          else
          {
            rfile.ImportReplace(_resource.Resources, ref pal, rootDir, p);
          }
        });
        //t.Wait();

        pbarProgress.Value = pbarProgress.Maximum;
        lblProgress.Text = "Finished!";
        MessageBox.Show("Import completed!");
        DialogResult = DialogResult.OK;
        Close();
      }
      catch (AggregateException ex)
      {
        MessageBox.Show("Import failed.\n\nReason: {0}".F(ex.InnerException?.Message ?? ex.Message));
      }
      finally
      {
        pbarProgress.Value = 0;
        lblProgress.Text = "";
        Enabled = true;
      }
    }

    private void bClose_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void bFilepath_Click(object sender, EventArgs e)
    {
      if (ofdImport.ShowDialog() == DialogResult.OK)
      {
        tbFilepath.Text = ofdImport.FileName;
      }
    }
  }
}
