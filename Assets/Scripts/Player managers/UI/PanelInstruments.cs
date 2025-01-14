using UnityEngine;

public static class PanelInstruments
{
    public static Vector3 AlignPanel(GameObject canvas, GameObject panel, GameObject alignTo)
    {
        Vector3 position = Input.mousePosition;

        var canvasCorners = new Vector3[4];
        canvas.GetComponent<RectTransform>().GetWorldCorners(canvasCorners);

        var slotCorners = new Vector3[4];
        alignTo.GetComponent<RectTransform>().GetWorldCorners(slotCorners);

        var localCorners = new Vector3[4];
        panel.GetComponent<RectTransform>().GetLocalCorners(localCorners);

        float panelHeight;
        panelHeight = panel.GetComponent<RectTransform>().rect.height;

        // Изменение позиции окошка описания относительно ячейки
        // X
        // За левым краем (сместить вправо)
        if ((slotCorners[0].x - localCorners[2].x) < canvasCorners[0].x) 
            position.x = slotCorners[2].x;
        // За правым краем – дефолтное положение (сместить влево)
        else
            position.x = slotCorners[0].x - localCorners[2].x;
        // Y
        // За нижним краем (сместить вверх)
        if ((slotCorners[0].y - localCorners[2].y) < canvasCorners[0].y)
            position.y = slotCorners[0].y;
        // За верхним краем – дефолтное положение (сместить вниз)
        else
            position.y = slotCorners[2].y - localCorners[2].y;

        return position;
    }
}
