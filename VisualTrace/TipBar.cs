
using Eto.Drawing;
using Eto.Forms;

namespace VisualTrace;

// 一个时间比对条
public class TipBar : Drawable
{
    public TipBar()
    {
        Size = new Size(120, 10); // 控件大小
        BackgroundColor = Colors.Transparent; // 背景颜色透明
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;

        var totalWidth = Width;
        var height = Height / 4;  // 色块高度
        var yPosition = Height - height - 4; // 计算底部的 y 坐标
        
        // 颜色分段
        var greenWidth = totalWidth / 3;
        var yellowWidth = totalWidth / 3;
        var redWidth = totalWidth - greenWidth - yellowWidth;

        // 画颜色块
        graphics.FillRectangle(Color.Parse("#a5d486"), 0, yPosition, greenWidth, height);
        graphics.FillRectangle(Color.Parse("#ffcc63"), greenWidth, yPosition, yellowWidth, height);
        graphics.FillRectangle(Color.Parse("#fb6351"), greenWidth + yellowWidth, yPosition, redWidth, height);

        // 画刻度线
        var smallFont = new Font("", 8); // 设置字体
        graphics.DrawText(smallFont, Colors.Black, greenWidth - 15, yPosition - 12, UserSettings.YellowSpeed + "ms");
        graphics.DrawText(smallFont, Colors.Black, greenWidth + yellowWidth - 15, yPosition - 12, UserSettings.RedSpeed + "ms");
    }

    void DrawSpeedText()
    {
        
    }
}