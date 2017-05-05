using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;
using System.IO;


namespace MiddleWare.Views
{
    class DetailDocumentRendererDS : IDocumentRenderer
    {
        public void Render(FlowDocument doc, object data)
        {
            TableRowGroup group = doc.FindName("TableRowsDetails") as TableRowGroup;
            Style styleCell = doc.Resources["BorderedCell"] as Style;

            foreach (data_detailSource item in ((DetailInfoPrintDS)data).TableDetails)
            {
                TableRow row = new TableRow();

                TableCell cell = new TableCell(new Paragraph(new Run(item.item)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.full_item)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.result)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.unit)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.normal_low)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.normal_high)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run((item.indicate))));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                group.Rows.Add(row);
            }
        }
    }
    class DetailDocumentRendererPL : IDocumentRenderer
    {
        public void Render(FlowDocument doc, object data)
        {
            TableRowGroup group = doc.FindName("TableRowsDetails") as TableRowGroup;
            Style styleCell = doc.Resources["BorderedCell"] as Style;

            Image LVC_PAC_Image = doc.FindName("LVC_PAC_Image") as Image;
            LVC_PAC_Image.Source = ((DetailInfoPrintPL)data).PLPAC;

            foreach (data_detailSource item in ((DetailInfoPrintPL)data).TableDetails)
            {
                TableRow row = new TableRow();

                TableCell cell = new TableCell(new Paragraph(new Run(item.item)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.full_item)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.result)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.unit)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.normal_low)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run(item.normal_high)));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                cell = new TableCell(new Paragraph(new Run((item.indicate))));
                cell.Style = styleCell;
                row.Cells.Add(cell);

                group.Rows.Add(row);
            }
        }
    }
}
