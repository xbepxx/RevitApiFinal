using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApiFinal
{
    #region Плагин для вставки марок помещений
    [TransactionAttribute(TransactionMode.Manual)]
    public class RevitApiFinal : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            XYZ point = uidoc.Selection.PickPoint("Выберите комнату");
            Room roomins = GetRoomByPoint(doc, point); //определяем комнату, которой принадлежит выбранная точка
            ElementId roomId = roomins.Id;
            XYZ roomCenterins = GetElementCenter(roomins);

            var roomTagTypes = new FilteredElementCollector(doc)
                       .OfCategory(BuiltInCategory.OST_RoomTags)
                       .OfType<FamilySymbol>()
                       .FirstOrDefault();

            View view = new FilteredElementCollector(doc) //выбираем вид
                .OfClass(typeof(View))
                .OfType<View>()
                .Where(x => x.Name.Equals("Level 1"))
                .FirstOrDefault();

            UV center = new UV(roomCenterins.X, roomCenterins.Y); //передаём координаты в 2-х мерном пространстве
            Transaction transaction = new Transaction(doc, "Марки помещений");
            transaction.Start();
            doc.Create.NewRoomTag(new LinkElementId(roomId), center, view.Id);
            transaction.Commit();

            return Result.Succeeded;
        }
        #region Получаем точку вставки примерно по центру
        public XYZ GetElementCenter(Element element) //метод, получающий элемент и возвращающий точку
        {
            BoundingBoxXYZ boundin = element.get_BoundingBox(null); //"рамка" вокруг группы. В BoundingBoxXYZ min - левый нижний дальний угол, а max - правый верхний ближний
            return (boundin.Min + boundin.Max) / 2;
        }
        #endregion
        public Room GetRoomByPoint(Document doc, XYZ point) //ищем центр комнаты, который вибирает пользователь
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms); //фильтр по комнатам
            foreach (Element e in collector)
            {
                Room room = e as Room; //рекомендуемое преобразование
                if (room != null) //если преобразование успешно
                {
                    if (room.IsPointInRoom(point)) //если точка с заданными координатами попадает в комнату, то
                        return room;
                }
            }
            return null; //если не находим комнату, которой принадлежит точка, то null
        }

    }
    #endregion
}
