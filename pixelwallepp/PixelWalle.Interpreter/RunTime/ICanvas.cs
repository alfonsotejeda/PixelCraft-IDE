public interface ICanvas
{
    void Color(string color);
    void Size(int size);
    void DrawLine(int dx, int dy, int length);
    void DrawRectangle(int dx, int dy, int distance, int width, int height);
    void DrawCircle(int dx, int dy, int radius);
    void Fill();

    bool IsBrushColor(string color);
    bool IsBrushSize(int size);
    bool IsCanvasColor(string color);
    (int width, int height) GetCanvasSize();
    int GetColorCount(string color, int x1, int y1, int x2, int y2);
    void SetCursor(int x, int y);
}