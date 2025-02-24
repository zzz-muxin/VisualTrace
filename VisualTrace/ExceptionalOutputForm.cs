using Eto.Forms;
using Eto.Drawing;
using System.ComponentModel;

namespace VisualTrace;

public class ExceptionalOutputForm : Form
{
    private TextArea _outputContainer;

    public ExceptionalOutputForm()
    {
        Title = Resources.EXC_OUTPUT_FORM_TITLE;
        //Icon = new Icon("icon.png");
        MinimumSize = new Size(600, 450);
        Padding = 10;
        Closing += Form_Closing;
        
        var layout = new TableLayout
        {
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow(new TableCell(new Label
                {
                    Text = Resources.EXC_OUTPUT_FORM_PROMPT,
                    Width = 500,
                    Wrap = WrapMode.Word
                }, true)),
                new TableRow(_outputContainer = new TextArea { ReadOnly = true }) { ScaleHeight = true },
                new TableRow
                (
                    new TableCell(null, true),
                    new TableCell(new Button { Text = Resources.CLOSE, Command = new Command((_, _) => Close()) })
                )
            }
        };
        Content = layout;
    }

    private void Form_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Visible = false;
    }

    public void AppendOutput(string output)
    {
        _outputContainer.Text += output + Environment.NewLine;
    }
}