using System;
using System.Collections.Generic;
using System.Timers;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace PWOOGFrameWork
{
    /// <summary>
    /// Статический класс, описывающий игровые данные клиента
    /// </summary>
    public static class PWData
    {
        /*private static eListCollection el;

        public enum BinaryType { Int16, Int32, Int64, Single, Double, Byte, WString, String, Auto }
        public struct BType
        {
            public BinaryType Type;
            public int Count;
            public static implicit operator BinaryType(BType type)
            {
                return type.Type;
            }
            public BType(BinaryType type, int count = 1)
            {
                this.Type = type;
                this.Count = count;
            }
        }

        public class eList
        {
            public eList()
            {
            }

            public String listName;// . from config file
            public byte[] listOffset;  // . length from config file, values from elements.data
            public string[] elementFields; // . length & values from config file
            public BType[] elementTypes; // . length & values from config file
            public object[][] elementValues; // list.length from elements.data, elements.length from config file

            // return a field of an element in string representation
            public String GetValue(int ElementIndex, int FieldIndex)
            {
                if (FieldIndex > -1)
                {
                    Object value = elementValues[ElementIndex][FieldIndex];
                    BinaryType type = elementTypes[FieldIndex];

                    if (type == BinaryType.Int16)
                    {
                        return Convert.ToString((short)value);
                    }
                    if (type == BinaryType.Int32)
                    {
                        return Convert.ToString((int)value);
                    }
                    if (type == BinaryType.Int64)
                    {
                        return Convert.ToString((long)value);
                    }
                    if (type == BinaryType.Single)
                    {
                        return Convert.ToString((float)value);
                    }
                    if (type == BinaryType.Double)
                    {
                        return Convert.ToString((double)value);
                    }
                    if (type == BinaryType.Byte)
                    {
                        // Convert from byte[] to Hex String
                        byte[] b = (byte[])value;
                        return BitConverter.ToString(b);
                    }
                    if (type == BinaryType.WString)
                    {
                        Encoding enc = Encoding.GetEncoding("Unicode");
                        return enc.GetString((byte[])value).Split(new char[] { '\0' })[0];
                    }
                    if (type == BinaryType.String)
                    {
                        Encoding enc = Encoding.GetEncoding("GBK");
                        return enc.GetString((byte[])value).Split(new char[] { '\0' })[0];
                    }
                }
                return "";
            }
            // return the type of the field in string representation
            public BType GetType(int FieldIndex)
            {
                if (FieldIndex > -1)
                {
                    return elementTypes[FieldIndex];
                }
                throw new ArgumentException();
            }
            /// <summary>
            /// Add Item
            /// </summary>
            /// <param name="itemValues"></param>
            public void AddItem(Object[] itemValues)
            {
                Object[][] newValues = new Object[elementValues.Length + 1][];
                Array.Resize(ref elementValues, elementValues.Length + 1);
                elementValues[elementValues.Length - 1] = itemValues;
            }
        }
        public class eListCollection
        {
            public eListCollection(String elFile)
            {
                Lists = Load(elFile);
            }

            public short Version;
            public short Signature;
            public int ConversationListIndex;
            public String ConfigFile;
            public eList[] Lists;

            public String GetValue(int ListIndex, int ElementIndex, int FieldIndex)
            {
                return Lists[ListIndex].GetValue(ElementIndex, FieldIndex);
            }
            public BType GetType(int ListIndex, int FieldIndex)
            {
                return Lists[ListIndex].GetType(FieldIndex);
            }
            private BType ParseType(String str)
            {
                if (str.Contains("AUTO"))
                    return new BType(BinaryType.Auto);
                else if (str == "int16")
                    return new BType(BinaryType.Int16);
                else if (str == "int32")
                    return new BType(BinaryType.Int32);
                else if (str == "int64")
                    return new BType(BinaryType.Int64);
                else if (str == "float")
                    return new BType(BinaryType.Single);
                else if (str == "double")
                    return new BType(BinaryType.Double);
                else if (str.Contains("byte:"))
                    return new BType(BinaryType.Byte, Convert.ToInt32(str.Substring(5)));
                else if (str.Contains("wstring:"))
                    return new BType(BinaryType.WString, Convert.ToInt32(str.Substring(8)));
                else if (str.Contains("string:"))
                    return new BType(BinaryType.String, Convert.ToInt32(str.Substring(7)));
                throw new ArgumentException();
            }
            private Object readValue(BinaryReader br, BType type)
            {
                if (type == BinaryType.Int16)
                    return br.ReadInt16();
                if (type == BinaryType.Int32)
                    return br.ReadInt32();
                if (type == BinaryType.Int64)
                    return br.ReadInt64();
                if (type == BinaryType.Single)
                    return br.ReadSingle();
                if (type == BinaryType.Double)
                    return br.ReadDouble();
                if (type == BinaryType.Byte)
                    return br.ReadBytes(Convert.ToInt32(type.Count));
                if (type == BinaryType.WString)
                    return br.ReadBytes(Convert.ToInt32(type.Count));
                if (type == BinaryType.String)
                    return br.ReadBytes(Convert.ToInt32(type.Count));
                return null;
            }

            // returns an eList array with preconfigured fields from configuration file
            private eList[] loadConfiguration(String file)
            {
                StreamReader sr = new StreamReader(file);
                eList[] Li = new eList[Convert.ToInt32(sr.ReadLine())];
                try
                {
                    ConversationListIndex = Convert.ToInt32(sr.ReadLine());
                }
                catch
                {
                    ConversationListIndex = 58;
                }
                String line;
                for (int i = 0; i < Li.Length; i++)
                {
                    while ((line = sr.ReadLine()) == "") { }
                    Li[i] = new eList();
                    Li[i].listName = line;
                    Li[i].listOffset = null;
                    String offset = sr.ReadLine();
                    if (offset != "AUTO")
                        Li[i].listOffset = new byte[Convert.ToInt32(offset)];
                    Li[i].elementFields = sr.ReadLine().Split(new char[] { ';' });
                    string[] sTypes = sr.ReadLine().Split(new char[] { ';' });
                    Li[i].elementTypes = new BType[sTypes.Length];
                    for (int ii = 0; ii < sTypes.Length; ii++)
                        Li[i].elementTypes[ii] = ParseType(sTypes[ii]);
                }
                sr.Close();

                return Li;
            }
            public eList[] Load(String elFile)
            {
                eList[] Li = new eList[0];

                // open the element file
                using (BinaryReader br = new BinaryReader(File.OpenRead(elFile)))
                {
                    Version = br.ReadInt16();
                    Signature = br.ReadInt16();

                    // check if a corresponding configuration file exists
                    String[] configFiles = Directory.GetFiles(Environment.CurrentDirectory + "\\configs", "PW_*_v" + Version + ".cfg");
                    if (configFiles.Length <= 0)
                        throw new ArgumentException("No corressponding configuration file found!\nVersion: " + Version + "\nPattern: " + "configs\\PW_*_v" + Version + ".cfg");

                    // configure an eList array with the configuration file
                    ConfigFile = configFiles[0];
                    Li = loadConfiguration(ConfigFile);

                    // read the element file
                    for (int l = 0; l < Li.Length; l++)
                    {
                        // read offset
                        if (Li[l].listOffset != null)
                        {
                            // offset > 0
                            if (Li[l].listOffset.Length > 0)
                            {
                                Li[l].listOffset = br.ReadBytes(Li[l].listOffset.Length);
                            }
                        }
                        // autodetect offset (for list 20 & 100)
                        else
                        {
                            if (l == 20)
                            {
                                byte[] head = br.ReadBytes(4);
                                byte[] count = br.ReadBytes(4);
                                byte[] body = br.ReadBytes(BitConverter.ToInt32(count, 0));
                                byte[] tail = br.ReadBytes(4);
                                Li[l].listOffset = new byte[head.Length + count.Length + body.Length + tail.Length];
                                Array.Copy(head, 0, Li[l].listOffset, 0, head.Length);
                                Array.Copy(count, 0, Li[l].listOffset, 4, count.Length);
                                Array.Copy(body, 0, Li[l].listOffset, 8, body.Length);
                                Array.Copy(tail, 0, Li[l].listOffset, 8 + body.Length, tail.Length);
                            }
                            else if (l == 100)
                            {
                                byte[] head = br.ReadBytes(4);
                                byte[] count = br.ReadBytes(4);
                                byte[] body = br.ReadBytes(BitConverter.ToInt32(count, 0));
                                Li[l].listOffset = new byte[head.Length + count.Length + body.Length];
                                Array.Copy(head, 0, Li[l].listOffset, 0, head.Length);
                                Array.Copy(count, 0, Li[l].listOffset, 4, count.Length);
                                Array.Copy(body, 0, Li[l].listOffset, 8, body.Length);
                            }
                        }

                        // read conversation list
                        if (l == ConversationListIndex)
                        {
                            // Auto detect only works for Perfect World elements.data !!!
                            if (Li[l].elementTypes[0] == BinaryType.Auto)
                            {
                                byte[] pattern = (Encoding.GetEncoding("GBK")).GetBytes("facedata\\");
                                long sourcePosition = br.BaseStream.Position;
                                int listLength = -72 - pattern.Length;
                                bool run = true;
                                while (run)
                                {
                                    run = false;
                                    for (int i = 0; i < pattern.Length; i++)
                                    {
                                        listLength++;
                                        if (br.ReadByte() != pattern[i])
                                        {
                                            run = true;
                                            break;
                                        }
                                    }
                                }
                                br.BaseStream.Position = sourcePosition;
                                Li[l].elementTypes[0] = new BType(BinaryType.Byte, listLength);
                            }

                            Li[l].elementValues = new Object[1][];
                            Li[l].elementValues[0] = new Object[Li[l].elementTypes.Length];
                            Li[l].elementValues[0][0] = readValue(br, Li[l].elementTypes[0]);
                        }
                        // read lists
                        else
                        {
                            Li[l].elementValues = new Object[br.ReadInt32()][];

                            // go through all elements of a list
                            for (int e = 0; e < Li[l].elementValues.Length; e++)
                            {
                                Li[l].elementValues[e] = new Object[Li[l].elementTypes.Length];

                                // go through all fields of an element
                                for (int f = 0; f < Li[l].elementValues[e].Length; f++)
                                {
                                    Li[l].elementValues[e][f] = readValue(br, Li[l].elementTypes[f]);
                                }
                            }
                        }
                    }
                }

                return Li;
            }

        }

        public struct MobType
        {
            public string Name;
            public string Element;
            public ushort Level;
            public uint HP;

            public uint PhysicalDefence { get; set; }
            /// <summary>
            /// Точность
            /// </summary>
            public uint Accuracy { get; set; } //SharpShooter :D
            /// <summary>
            /// Минимальный урон от физической атаки
            /// </summary>
            public uint PhysicalAttackMin { get; set; }
            /// <summary>
            /// Максимальный урон от физической атаки
            /// </summary>
            public uint PhysicalAttackMax { get; set; }
            /// <summary>
            /// Минимальный урон от магической атаки
            /// </summary>
            public uint MagicAttackMin { get; set; }
            /// <summary>
            /// Максимальный урон от магической атаки
            /// </summary>
            public uint MagicAttackMax { get; set; }
            /// <summary>
            /// Защита от металла
            /// </summary>
            public uint MetalDefence { get; set; }
            /// <summary>
            /// Защита от дерева
            /// </summary>
            public uint TreeDefence { get; set; }
            /// <summary>
            /// Защита от воды
            /// </summary>
            public uint WaterDefence { get; set; }
            /// <summary>
            /// Защита от огня
            /// </summary>
            public uint FireDefence { get; set; }
            /// <summary>
            /// Защита от земли
            /// </summary>
            public uint LandDefence { get; set; }
            /// <summary>
            /// Уклонение
            /// </summary>
            public uint Evasion { get; set; }
        }

        public string GetOccupation(uint id)
        {
            try
            {
                return (from t in el.Lists[71].elementValues where (Int32)t[0] == id select (string)t[1]).ElementAt(0);
            }
            catch { throw; }
        }
        public uint GetRace(uint id)
        {
            try
            {
                return (uint)(from t in el.Lists[71].elementValues where (Int32)t[0] == id select (Int32)t[3]).ElementAt(0);
            }
            catch { throw; }
        }

        public MobType GetMobType(uint type)
        {
            var _r = (from t in el.Lists[38].elementValues where (Int32)t[0] == type select t).ElementAt(0);
            MobType r = new MobType { Name = (string)_r[2],
                                      Element = (string)_r[4],
                                      Level = (ushort)((Int32)_r[14]),
                                      HP = (uint)((Int32)_r[17]),
                                      PhysicalDefence = (uint)((Int32)_r[18]),
                                      MetalDefence = (uint)((Int32)_r[19]),
                                      TreeDefence = (uint)((Int32)_r[20]),
                                      WaterDefence = (uint)((Int32)_r[21]),
                                      FireDefence = (uint)((Int32)_r[22]),
                                      LandDefence = (uint)((Int32)_r[23])
            };
            return r;
        }
        */

        /// <summary>
        /// Эмоции
        /// </summary>
        public enum Emote : ushort { Помахать, Кивнуть, ПокачатьГоловой, ПожатьПлечами, Рассмеяться, Рассердиться, Головокружение, Грустить, ВоздушныйПоцелуй, Застестняться, Поблагодарить, Сесть, Зарядка, Думать, Насмехаться, Победа, Потянуться, Бой, Атака1, Атака2, Атака3, Атака4, Защита, Упасть, ПритворнаяСмерть, Оглядеться, Танцевать }
        /// <summary>
        /// Заклинания
        /// </summary>
        public static SortedDictionary<string, ushort> Skills;
        /// <summary>
        /// Монстры
        /// </summary>
        public static SortedDictionary<string, ushort> Mobs;

        static PWData()
        {
            //el = new eListCollection(@"D:\ИЛЬЯ\игры\Perfect World\element\data\elements.data");
        }
    }

    /// <summary>
    /// Персонаж для выбора
    /// </summary>
    public struct Char
    {
        /// <summary>
        /// Ид
        /// </summary>
        public uint Id;
        /// <summary>
        /// Пол
        /// </summary>
        public byte Gender;
        /// <summary>
        /// Раса
        /// </summary>
        public byte Race;
        /// <summary>
        /// Класс / профессия
        /// </summary>
        public byte Occupation;
        /// <summary>
        /// Уровень
        /// </summary>
        public uint Level;
        /// <summary>
        /// Имя
        /// </summary>
        public string Name;
    }
    /// <summary>
    /// Тип игрового объекта
    /// </summary>
    public enum GameObjectType
    {
        /// <summary>
        /// Монстр
        /// </summary>
        Mob,
        /// <summary>
        /// Игрок
        /// </summary>
        Player,
        /// <summary>
        /// Игрок, продающий товары
        /// </summary>
        Market
    }
    /// <summary>
    /// Игровой объект
    /// </summary>
    public struct GameObject
    {
        /// <summary>
        /// Тип объекта
        /// </summary>
        public GameObjectType Type { get; set; }

        /// <summary>
        /// Движется ли объект
        /// </summary>
        public bool IsMoving;

        /// <summary>
        /// Локация
        /// </summary>
        public Point3 Location { get; set; }

        /// <summary>
        /// Уровень персонажа
        /// </summary>
        public ushort Level { get; set; }
        /// <summary>
        /// Процент здоровья
        /// </summary>
        public uint HP { get; set; }
        /// <summary>
        /// Процент маны
        /// </summary>
        public uint MP { get; set; }
        /// <summary>
        /// Максимальный процент здоровья
        /// </summary>
        public uint MaxHP { get; set; }
        /// <summary>
        /// Максимальный процент маны
        /// </summary>
        public uint MaxMP { get; set; }
    }
    /// <summary>
    /// Тип сообщения для чата
    /// </summary>
    public enum ChatMessageType
    {
        /// <summary>
        /// Приватное сообщение (персонаж персонажу)
        /// </summary>
        Private,
        /// <summary>
        /// Локальное сообщение (вокруг персонажа)
        /// </summary>
        Local,
        /// <summary>
        /// Глобальное сообщение (мировое)
        /// </summary>
        World
    }
    /// <summary>
    /// Сообщение
    /// </summary>
    public struct ChatMessage
    {
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public ChatMessageType Type;
        /// <summary>
        /// Ник получателя
        /// </summary>
        public string ReceiverNick;
        /// <summary>
        /// Ид получателя
        /// </summary>
        public uint ReceiverUID;
        /// <summary>
        /// Ник отправителя
        /// </summary>
        public string SenderNick;
        /// <summary>
        /// Ид отправителя
        /// </summary>
        public uint SenderUID;
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message;
    }
    /// <summary>
    /// Условие продажи или покупки предмета
    /// </summary>
    public struct TradeTerm
    {
        /// <summary>
        /// Предмет, выставляемый на продажу
        /// </summary>
        public InventoryItem Item { get; set; }
        /// <summary>
        /// Количество
        /// </summary>
        public uint Count { get; set; }
        /// <summary>
        /// Цена
        /// </summary>
        public uint Price { get; set; }

        /// <summary>
        /// Объявляет структуру, описывающую условие продажи предмета
        /// </summary>
        /// <param name="item">Предмет</param>
        /// <param name="count">Количество</param>
        /// <param name="price">Цена</param>
        public TradeTerm(InventoryItem item, uint count, uint price)
            : this()
        {
            if (count > item.Count)
                throw new ArgumentException("Количество предметов на продажу не может превышать физическое кол-во предметов!");
            this.Item = item;
            this.Count = count;
            this.Price = price;
        }
        /// <summary>
        /// Объявляет структуру, описывающую условие покупки предмета
        /// </summary>
        /// <param name="id">Ид предмета</param>
        /// <param name="count">Количество</param>
        /// <param name="price">Цена</param>
        public TradeTerm(uint id, uint count, uint price) : this()
        {
            this.Item = new InventoryItem { Id = id, Slot = ushort.MaxValue };
            this.Count = count;
            this.Price = price;
        }
        /*/// <summary>
        /// Объявляет структуру, описывающую условие покупки предмета
        /// </summary>
        /// <param name="item">Предмет</param>
        /// <param name="count">Количество</param>
        /// <param name="price">Цена</param>
        public TradeTerm(InventoryItem item, uint count, uint price)
        {
            if (item.IsEmpty)
                throw new EmptyItemException();
            this.Item = new InventoryItem { Id = id, Slot = ushort.MaxValue };
            this.Count = count;
            this.Price = price;
        }*/
    }
    /// <summary>
    /// Предмет инвентаря
    /// </summary>
    public struct InventoryItem
    {
        /// <summary>
        /// Пуст ли слот
        /// </summary>
        public bool IsEmpty;
        /// <summary>
        /// Номер слота, в котором находится предмет
        /// </summary>
        public uint Slot { get; set; }

        /// <summary>
        /// !!! Будет заменено !!! Тип инвентаря, в котором находится предмет
        /// </summary>
        public byte Type;

        /// <summary>
        /// Ид
        /// </summary>
        public uint Id;
        /// <summary>
        /// Количество
        /// </summary>
        public ushort Count;
    }
    /// <summary>
    /// Инвентарь
    /// </summary>
    public class InventoryList
    {
        /// <summary>
        /// Получены ли данные инвентаря
        /// </summary>
        public bool IsReceived { get; set; }
        /// <summary>
        /// Словарь, который желательно использовать для поиска предметов по фильтру, но не более
        /// </summary>
        public Dictionary<byte, InventoryItem> List { get; private set; }
        private Action<byte, byte> onSwap;
        private Action<byte, byte, ushort> onSplit;
        private Action<ushort, uint> onUse;
        private Action<string, TradeTerm[]> onTrade;

        private bool tryGetKey(InventoryItem item, out byte key)
        {
            foreach (var elm in List)
                if (elm.Value.Equals(item))
                {
                    key = elm.Key;
                    return true;
                }
            key = default(byte);
            return false;
        }
        private bool tryGetEmptySlot(out byte key)
        {
            foreach (var elm in List)
                if (elm.Value.IsEmpty)
                {
                    key = elm.Key;
                    return true;
                }
            key = default(byte);
            return false;
        }

        /// <summary>
        /// Объявляет класс, описывающий инвентарь
        /// </summary>
        /// <param name="onSwap">Функция перемещения предметов</param>
        /// <param name="onSplit">Функция разделения предметов</param>
        /// <param name="onUse">Функция использования предметов</param>
        /// <param name="onTrade">Функция продажи предметов</param>
        public InventoryList(Action<byte, byte> onSwap, Action<byte, byte, ushort> onSplit, Action<ushort, uint> onUse, Action<string, TradeTerm[]> onTrade)
        {
            this.onSwap = onSwap;
            this.onSplit = onSplit;
            this.onUse = onUse;
            this.onTrade = onTrade;
        }
        public InventoryItem this[byte pos]
        {
            get
            {
                //if (List[pos].IsEmpty)
                //    throw new ArgumentException("Слот по указанной позиции пуст!");
                return List[pos];
            }
            private set { }
        }

        public void SetCapacity(byte capacity)
        {
            List = new Dictionary<byte, InventoryItem>();
            for (byte i = 0; i < capacity; i++)
                List[i] = new InventoryItem { IsEmpty = true };
        }
        public void Insert(byte pos, InventoryItem item)
        {
            List[pos] = item;
        }

        /// <summary>
        /// Ждёт получения данных инвентаря
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns>Получены ли данные</returns>
        /// </summary>
        public bool WaitObtaining(int time = 10)
        {
            for (int i = 0; i < time * 10; i++)
            {
                System.Threading.Thread.Sleep(100);
                if (IsReceived)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Меняет предметы местами
        /// </summary>
        /// <param name="pos1">Номер слота 1</param>
        /// <param name="pos2">Номер слота 2</param>
        /// <exception cref="TwoSlotsEmptyException" />
        public void Swap(byte pos1, byte pos2)
        {
            try
            {
                if (List[pos1].IsEmpty && List[pos2].IsEmpty)
                    throw new TwoSlotsEmptyException();
                onSwap(pos1, pos2);

                //swap items
                var tmp1 = List[pos1];
                List[pos1] = List[pos2];
                List[pos2] = tmp1;

                //swap Slots
                var item1 = List[pos1];
                var item2 = List[pos2];
                var tmp2 = item1.Slot;
                item1.Slot = item2.Slot;
                item2.Slot = tmp2;
                List[pos1] = item1;
                List[pos2] = item2;
            }
            catch { throw; }
        }
        /// <summary>
        /// Меняет предметы местами
        /// </summary>
        /// <param name="item">Предмет</param>
        /// <param name="pos">Номер слота</param>
        /// <exception cref="EmptyItemException" />
        /// <exception cref="ItemNotFoundException" />
        /// <exception cref="TwoSlotsEmptyException" />
        public void Swap(InventoryItem item, byte pos)
        {
            try
            {
                if (item.IsEmpty)
                    throw new EmptyItemException();
                if (!List[(byte)item.Slot].Equals(item))
                    throw new ItemNotFoundException();
                onSwap((byte)item.Slot, pos);
            }
            catch { }
        }
        /// <summary>
        /// Меняет предметы местами
        /// </summary>
        /// <param name="item1">Предмет 1</param>
        /// <param name="item2">Предмет 2</param>
        /// <exception cref="EmptyItemException" />
        /// <exception cref="ItemNotFoundException" />
        /// <exception cref="TwoSlotsEmptyException" />
        public void Swap(InventoryItem item1, InventoryItem item2)
        {
            try
            {
                if (item1.IsEmpty)
                    throw new EmptyItemException();
                if (item2.IsEmpty)
                    throw new EmptyItemException();
                if (!List[(byte)item1.Slot].Equals(item1))
                    throw new ItemNotFoundException();
                if (!List[(byte)item2.Slot].Equals(item2))
                    throw new ItemNotFoundException();
                onSwap((byte)item1.Slot, (byte)item2.Slot);
            }
            catch { throw; }
        }


        /// <summary>
        /// Разделяет предмет
        /// </summary>
        /// <param name="pos1">Слот предмета для разделения</param>
        /// <param name="pos2">Позиция новой порции</param>
        /// <param name="count">Количество предметов в новой порции</param>
        /// <exception cref="TwoSlotsEmptyException" />
        /// <exception cref="SlotOccupiedException" />
        /// <exception cref="ArgumentException" />
        public void Split(byte pos1, byte pos2, ushort count)
        {
            try
            {
                byte item1;
                byte item2;
                item1 = pos1;
                if (List[item1].IsEmpty)
                {
                    item1 = pos2;
                    if (List[item1].IsEmpty)
                        throw new TwoSlotsEmptyException();
                    item2 = pos1;
                }
                else
                    item2 = pos2;
                if (!List[pos1].IsEmpty && !List[pos2].IsEmpty)
                    throw new SlotOccupiedException();
                if (List[item1].Count <= count)
                    throw new ArgumentException("Количество предметов в новой порции не должно превышать количество либо быть ему равным!");
                if (count < 0)
                    throw new ArgumentException("Количество предметов в новой порции должно быть положительным!");

                Split(pos1, pos2, count);

                var item = List[item1];
                item.Count -= count;
                List[item1] = item;
                List[item2] = new InventoryItem { IsEmpty = false, Slot = item2, Type = item.Type, Id = item.Id, Count = count };
            }
            catch { throw; }
        }
        /// <summary>
        /// Разделяет предмет, порция помещается в первый пустой слот
        /// </summary>
        /// <param name="pos">Слот предмета для разделения</param>
        /// <param name="count">Количество предметов в новой порции</param>
        /// <exception cref="TwoSlotsEmptyException" />
        /// <exception cref="AllSlotsOccupiedException" />
        /// <exception cref="ArgumentException" />
        public void Split(byte pos, ushort count)
        {
            try
            {
                byte eSlot;
                if (!tryGetEmptySlot(out eSlot))
                    throw new AllSlotsOccupiedException();
                Split(pos, eSlot, count);
            }
            catch { throw; }
        }
        /// <summary>
        /// Разделяет предмет
        /// </summary>
        /// <param name="item">Предмет для разделения</param>
        /// <param name="pos">Позиция новой порции</param>
        /// <param name="count">Количество предметов в новой порции</param>
        /// <exception cref="EmptyItemException" />
        /// <exception cref="ItemNotFoundException" />
        /// <exception cref="SlotOccupiedException" />
        /// <exception cref="ArgumentException" />
        public void Split(InventoryItem item, byte pos, ushort count)
        {
            try
            {
                if (item.IsEmpty)
                    throw new EmptyItemException();
                if (!List[(byte)item.Slot].Equals(item))
                    throw new ItemNotFoundException();
                Split((byte)item.Slot, pos, count);
            }
            catch { throw; }
        }
        /// <summary>
        /// Разделяет предмет, порция помещается в первый пустой слот
        /// </summary>
        /// <param name="item">Предмет для разделения</param>
        /// <param name="count">Количество предметов в новой порции</param>
        /// <exception cref="EmptyItemException" />
        /// <exception cref="ItemNotFoundException" />
        /// <exception cref="AllSlotsOccupiedException" />
        /// <exception cref="ArgumentException" />
        public void Split(InventoryItem item, ushort count)
        {
            try
            {
                if (item.IsEmpty)
                    throw new EmptyItemException();
                if (!List[(byte)item.Slot].Equals(item))
                    throw new ItemNotFoundException();

                Split((byte)item.Slot, count);
            }
            catch { throw; }
        }


        /// <summary>
        /// Использует предмет
        /// </summary>
        /// <param name="item">Предмет</param>
        public void Use(InventoryItem item)
        {
            try
            {
                byte key;
                if (!tryGetKey(item, out key))
                    throw new ItemNotFoundException();

                onUse(key, item.Id);
            }
            catch { throw; }
        }
        /// <summary>
        /// Использует предмет
        /// </summary>
        /// <param name="pos">Предмет</param>
        public void Use(byte pos)
        {
            try
            {
                var item = List[pos];
                if (item.IsEmpty)
                    throw new EmptyItemException();

                if (--item.Count <= 0)
                    item = new InventoryItem { IsEmpty = true };
                onUse(pos, item.Id);
            }
            catch { throw; }
        }

        /// <summary>
        /// Выставляет предметы на продажу
        /// </summary>
        /// <param name="name">Название торговца (11 символов)</param>
        /// <param name="items">Предметы для продажи</param>
        /// <exception cref="LockActiveException" />
        public void Trade(string name, params TradeTerm[] items)
        {
            try
            {
                if (name.Length == 0)
                    throw new EmptyStringException();
                if (name.Length > 11)
                    throw new LongStringException(11);
                foreach (var elm in items)
                {
                    if (elm.Item.IsEmpty)
                        throw new EmptyItemException();
                    if (!List[(byte)elm.Item.Slot].Equals(elm.Item))
                        throw new ItemNotFoundException();
                }
                onTrade(name, items);
            }
            catch { throw; }
        }
    }


    /// <summary>
    /// Исключение, выбрасываемое при попытке передачи слишком длинной строки
    /// </summary>
    public class LongStringException : Exception
    {
        public LongStringException(int maxLength)
            : base(string.Format("Максимальная длина строки составляет {0} символа/ов!", maxLength)) { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при попытке передачи пустой строки
    /// </summary>
    public class EmptyStringException : Exception
    {
        public EmptyStringException()
            : base("Строка не должна быть пустой!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при попытке передачи пустого предмета
    /// </summary>
    public class EmptyItemException : Exception
    {
        public EmptyItemException()
            : base("Предмет не существует!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при попытке передачи предмета, которого нет в инвентаре
    /// </summary>
    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException()
            : base("Предмета нет в инвентаре!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при отсутствии пустого слота
    /// </summary>
    public class AllSlotsOccupiedException : Exception
    {
        public AllSlotsOccupiedException()
            : base("Нету свободных слотов!") { }
    }
    /// <summary>
    /// исключение, выбрасываемое при попытке передачи 2-ух пустых слотов
    /// </summary>
    public class TwoSlotsEmptyException : Exception
    {
        public TwoSlotsEmptyException()
            : base("Оба слота пусты!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при попытке передать занятый слот
    /// </summary>
    public class SlotOccupiedException : Exception
    {
        public SlotOccupiedException()
            : base("Слот занят!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при попытке использования функции, предназначенной для мёртвого персонажа!
    /// </summary>
    public class PersAliveException : Exception
    {
        public PersAliveException()
            : base("Персонаж ещё жив!") { }
    }
    /// <summary>
    /// Исключение, выбрасываемое при недостаточном количестве очков распределения навыков.     
    /// </summary>
    public class FewStatsException : Exception
    {
        public FewStatsException()
            : base("Недостаточно очков распределения навыков") { }
    }
    /// <summary>
    /// Тип возрождения
    /// </summary>
    public enum RessurectType
    {
        /// <summary>
        /// Возрождение в городе
        /// </summary>
        ToTown,
        /// <summary>
        /// Возрождение на месте с использованием свитка
        /// </summary>
        UseScroll,
        /// <summary>
        /// Возрождение на месте с использованием бафа на возрождение
        /// </summary>
        UseBuff
    }


    /// <summary>
    /// Класс, реализующий сетевое взаимодействие с сервером PW, абсорбирующий детали протокола PW от пользователя
    /// </summary>
    public partial class PWClient
    {
        //Внутренние данные бота
        /// <summary>
        /// Объект, который следует блокировать при обработке информации класса
        /// </summary>
        public object Wrapper { get; private set; }
        private byte[] Key;
        private byte[] Hash;
        private byte Force;
        private uint AccountKey;
        private Timer KeepAliveTimer;
        //Данные для входа
        /// <summary>
        /// Логин
        /// </summary>
        public string Login { get; private set; }
        /// <summary>
        /// Пароль
        /// </summary>
        public string Password { get; private set; }
        /// <summary>
        /// Адрес сервера
        /// </summary>
        public string ServerAddress { get; private set; }
        //Login
        /// <summary>
        /// Выполняет подключение к серверу
        /// </summary>
        /// <returns>Вовзращает true или выбивает Exception</returns>
        public bool Connect()
        {
            this.connect(ServerAddress);
            return true;
        }
        /// <summary>
        /// Совершена ли попытка авторизации
        /// </summary>
        public bool IsLoginCompleted { get; private set; }
        /// <summary>
        /// Событие, вызызаемое при получении ответа о результате авторизации
        /// </summary>
        public event Action<bool> LoginCompleted;
        /// <summary>
        /// Результат авторизации
        /// </summary>
        public bool LoginResult { get; private set; }
        //Chars
        /// <summary>
        /// Получены ли персонажи для входа в игру
        /// </summary>
        public bool IsCharsReceived { get; private set; }
        /// <summary>
        /// Событие, вызываемое при получении персонажей для входа в игру
        /// </summary>
        public event Action CharsReceived;
        /// <summary>
        /// Список персонажей для входа в игру
        /// </summary>
        public List<Char> Chars { get; private set; }
        //IsInGame
        /// <summary>
        /// Выбирает персонажа для входа в игру
        /// </summary>
        /// <param name="selectedChar">Выбираемый персонаж</param>
        public void SelectChar(Char selectedChar)
        {
            if (!Chars.Contains(selectedChar))
                throw new ArgumentException("Данного персонажа не существует");
            this.SelectedChar = selectedChar;
            p_SendSelectRole();
        }
        /// <summary>
        /// Выбранный персонаж
        /// </summary>
        public Char SelectedChar { get; private set; }
        /// <summary>
        /// Находится ли выбранный персонаж в игровом мире
        /// </summary>
        public bool IsInGame { get; private set; }
        /// <summary>
        /// Событие, вызвываемое при входе выбранного персонажа в игровой мир
        /// </summary>
        public event Action InGame;
        //Disconnect
        /// <summary>
        /// Произошёл ли разрыв связи
        /// </summary>
        public bool IsDisconnected { get; private set; }
        /// <summary>
        /// Событие, вызываемое при разрыве связи с сервером
        /// </summary>
        public event Action Disconnected;

        //Ступени авторизации_Wait
        /// <summary>
        /// Ожидает результата авторизации
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns>Получен ли результат</returns>
        public bool WaitLoginResult(int time = 10)
        {
            if (IsLoginCompleted)
                return true;
            for (int i = 0; i < time * 10; i++)
            {
                System.Threading.Thread.Sleep(100);
                if (IsLoginCompleted)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Ожидает получения персонажей
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns>Получены ли персонажи</returns>
        public bool WaitCharsObtaining(int time = 10)
        {
            if (IsCharsReceived)
                return true;
            try
            {
                for (int i = 0; i < time * 10; i++)
                {
                    System.Threading.Thread.Sleep(100);
                    if (IsCharsReceived)
                        return true;
                }
            }
            catch (Exception exc)
            {
                System.Windows.Forms.MessageBox.Show(exc.Message);
                //exc.GetHashCode();
            }
            return false;
        }
        /// <summary>
        /// Ожидает входа в игру
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns>Осуществлён ли вход в игру</returns>
        public bool WaitEntryInGame(int time = 10)
        {
            if (IsInGame)
                return true;
            for (int i = 0; i < time * 10; i++)
            {
                System.Threading.Thread.Sleep(100);
                if (IsInGame)
                    return true;
            }
            return false;
        }

        //Игра -> Стуктура игрока
        /// <summary>
        /// Положение игрока
        /// </summary>
        public Point3 Location { get; private set; }
        /// <summary>
        /// Уровень персонажа
        /// </summary>
        public ushort Level { get; private set; }
        /// <summary>
        /// Очки опыта
        /// </summary>
        public uint Expirience { get; private set; }
        /// <summary>
        /// Количество юаней
        /// </summary>
        public uint Money { get; private set; }
        /// <summary>
        /// Инвентарь
        /// </summary>
        public InventoryList Inventory { get; private set; }

        #region Lock
        /// <summary>
        /// Включена ли персональная блокировка персонажа
        /// </summary>
        public bool IsLockEnable { get; private set; }
        private bool _isLocked;
        /// <summary>
        /// Показывает, прошло ли время действия блокировки
        /// </summary>
        public bool IsLockActive
        {
            get
            {
                if (!_isLocked)
                    return _isLocked;
                if (!IsLockEnable || DateTime.Now > UnlockTime)
                {
                    _isLocked = false;
                    return false;
                }
                return true;
            }
            private set { _isLocked = value; }
        }
        /// <remarks>Время указано относительно установленного на компьютере!</remarks>
        /// <summary>
        /// Время конца действия блокировки
        /// </summary>
        public DateTime UnlockTime { get; private set; }
        /// <summary>
        /// Длительность действия блокировки в секундах
        /// </summary>
        public uint LockDuration { get; private set; }

        /// <summary>
        /// Ожидает конца блокировки
        /// </summary>
        public void WaitLock()
        {
            if (!IsLockActive)
                return;
            var d = (int)(UnlockTime - DateTime.Now).TotalMilliseconds;
            System.Threading.Thread.Sleep(d);
        }
        /// <summary>
        /// Ожидает конца блокировки
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns>Снялась ли блокировка</returns>
        public bool WaitLock(int time)
        {
            if (!IsLockActive)
                return true;
            System.Threading.Thread.Sleep(time * 1000);
            return IsLockActive;
        }
        #endregion

        #region Фулл Стата
        /// <summary>
        /// Доступные для распределения очки
        /// </summary>
        public uint FreeStats { get; private set; }
        /// <summary>
        /// Шанс критического удара
        /// </summary>
        public uint CriticalChance { get; private set; }
        /// <summary>
        /// Сила духа
        /// </summary>
        public uint SpiritPower { get; private set; }
        /// <summary>
        /// Очки невидимости
        /// </summary>
        public uint Invisible { get; private set; }
        /// <summary>
        /// Очки обнаружения
        /// </summary>
        public uint TrueSight { get; private set; }
        /// <summary>
        /// Урон монстрам
        /// </summary>
        public uint MonsterDamage { get; private set; }
        /// <summary>
        /// Защита от монстров
        /// </summary>
        public uint MonsterDefence { get; private set; }
        /// <summary>
        /// Выносливость
        /// </summary>
        public uint Vitality { get; private set; }
        /// <summary>
        /// Интеллект
        /// </summary>
        public uint Intellect { get; private set; }
        /// <summary>
        /// Сила
        /// </summary>
        public uint Strenght { get; private set; }
        /// <summary>
        /// Ловкость
        /// </summary>
        public uint Agility { get; private set; }
        /// <summary>
        /// Сила критического удара
        /// </summary>
        public uint CriticalDamage { get; private set; }
        /// <summary>
        /// Скорость
        /// </summary>
        public uint Speed { get; private set; }
        /// <summary>
        /// Точность
        /// </summary>
        public uint Accuracy { get; private set; } //SharpShooter :D
        /// <summary>
        /// Минимальный урон от физической атаки
        /// </summary>
        public uint PhysicalAttackMin { get; private set; }
        /// <summary>
        /// Максимальный урон от физической атаки
        /// </summary>
        public uint PhysicalAttackMax { get; private set; }
        /// <summary>
        /// Минимальный урон от магической атаки
        /// </summary>
        public uint MagicAttackMin { get; private set; }
        /// <summary>
        /// Максимальный урон от магической атаки
        /// </summary>
        public uint MagicAttackMax { get; private set; }
        /// <summary>
        /// Защита от металла
        /// </summary>
        public uint MetalDefence { get; private set; }
        /// <summary>
        /// Защита от дерева
        /// </summary>
        public uint TreeDefence { get; private set; }
        /// <summary>
        /// Защита от воды
        /// </summary>
        public uint WaterDefence { get; private set; }
        /// <summary>
        /// Защита от огня
        /// </summary>
        public uint FireDefence { get; private set; }
        /// <summary>
        /// Защита от земли
        /// </summary>
        public uint LandDefence { get; private set; }
        /// <summary>
        /// Уклонение
        /// </summary>
        public uint Evasion { get; private set; }
        /// <summary>
        /// Магическая защита
        /// </summary>
        public uint MagicDefence { get; private set; }

        //Ну просто стата...
        /// <summary>
        /// Процент здоровья
        /// </summary>
        public uint HP { get; private set; }
        /// <summary>
        /// Процент маны
        /// </summary>
        public uint MP { get; private set; }
        /// <summary>
        /// Максимальный процент здоровья
        /// </summary>
        public uint MaxHP { get; private set; }
        /// <summary>
        /// Максимальный процент маны
        /// </summary>
        public uint MaxMP { get; private set; }

        /// <summary>
        /// Максимальное кол-во очков ярости
        /// </summary>
        public uint MaxRage { get; private set; }
        /// <summary>
        /// Максильное кол-во духа
        /// </summary>
        public uint MaxSpirit { get; private set; }
        /// <summary>
        /// Кол-во очков ярости
        /// </summary>
        public uint Rage { get; private set; }
        /// <summary>
        /// Кол-во очков духа
        /// </summary>
        public uint Spirit { get; private set; }
        #endregion

        /// <summary>
        /// Событие, вызываемое при получении сообщения из игрового мира
        /// </summary>
        public event Action<ChatMessage> ChatMessageReceived;
        /// <summary>
        /// Событие, вызываемое при получении желаемой цели от сервера (Кто-то начал бить вас)
        /// </summary>
        public event Action<uint> AutoTarget;
        /// <summary>
        /// Событие, вызываемое при получении информации о характеристиках выбранной цели
        /// </summary>
        public event Action<GameObject> TargetInfo;


        //!+0568 EquipWeapon
        //!+056C EquipHelmet
        //!+0570 EquipNecklace
        //!+0574 EquipManteau
        //!+0578 EquipShirt
        //!+057C EquipWaistAdorn
        //!+0580 EquipFootwear
        //!+0584 EquipBoots
        //!+058C EquipRing1
        //!+0590 EquipRing2
        //!+0598 EquipFly
        //!+059С Equip BodyFashion
        //!+05A0 Equip Legwear Fashion
        //!+05A4 Equip Special Footwears
        //!+05A8 Equip Arm Fashion
        //!+05AC Equip Head
        //+05B0 EquipSmiley / Книга
        //+05B8 Equip GuardianCharm / Амулет
        //+05BC Equip SpiritCharm / Идол
        //+05C0 EquipX1 / Сборник цитат
        //+05C4 EquipGenie / Джин
        //+05D0 EquipX2 / Жетон престижа
        //+05E0 Fame /Репутация/
        //+05EC FlagPeaceZone, byte
        //+05F0 FlagPK
        //+05F4 TimerPK
        //+061C ClanID, dword
        //+0630 flagGM, bit 00000001

        //Игра -> Мульиплэйер
        /// <summary>
        /// Отсортированный словарь игровых объектов (Мобы, Игроки)
        /// </summary>
        public SortedDictionary<uint, GameObject> GameObjects { get; private set; }
        /*public SortedDictionary<uint, Player> Players;
        public SortedDictionary<uint, Player> Mobs;*/

        /// <summary>
        /// Исключение, выбрасываемое при попытке совершить действие, запрещённое на время действия блокировки
        /// </summary>
        public class LockActiveException : Exception
        {
            public LockActiveException()
                : base("Время действия блокировки не закончилось!") { }
        }

        /// <summary>
        /// Инициализирует класс Client для взаимодействия с сервером
        /// </summary>
        /// <param name="serverAddr">Адрес сервера в формате [ip:port]</param>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        public PWClient(string serverAddr, string login, string password)
            : this() //Инициализатор расположен в Client_Net
        {
            Wrapper = new object();

            Login = login.ToLower();
            Password = password;
            ServerAddress = serverAddr;

            IsLoginCompleted = false;
            LoginResult = false;
            IsCharsReceived = false;
            IsInGame = false;

            IsLockActive = true;

            Inventory = new InventoryList(
                (byte pos1, byte pos2) => p_SwapItem(pos1, pos2),
                (byte pos1, byte pos2, ushort count) => p_SplitItem(pos1, pos2, count),
                (ushort slot, uint id) => p_UseItem(slot, id),
                (string name, TradeTerm[] items) =>
                {
                    if (IsLockActive)
                        throw new LockActiveException();
                    p_OpenMarketWindow();
                    p_OpenPersonalMarket(name, items);
                }
                );

            Chars = new List<Char>(10);
            Location = new Point3(0, 0, 0);

            //
            // Players = new SortedDictionary<uint, Player>();
            //Mobs = new SortedDictionary<uint, Player>();
            GameObjects = new SortedDictionary<uint, GameObject>();

            runTimer = new Timer(250);
            runTimer.Elapsed += (object obj, ElapsedEventArgs e) => { try { TMP(); } catch { } }; //try для выхода из потока

            chatQueue = new ActionQueueAsync(true);
        }

        //@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!
        //НУБОКОД, НЕ СМОТРЕТЬ! Я ПРЕДУПРЕЖАЛ!
        private Timer runTimer;
        private Point3 runFrom;
        private Point3 runTo;
        private float runSpeed = 3F;
        private ushort n;

        public void TMP()
        {
            //4F;
            float speed = runSpeed / 2; //(скорость 1м/сек), speed / MSEq = 0.5s

            float wayX = runTo.X - Location.X;
            float wayY = runTo.Y - Location.Y;
            float wayZ = runTo.Z - Location.Z;


            float lenX = Math.Abs(wayX);
            float lenY = Math.Abs(wayY);
            float lenZ = Math.Abs(wayZ);

            if (lenX + lenY + lenZ < 3)
            {
                runTimer.Enabled = false;
                runTimer.Stop();
                if (lenX + lenY + lenZ > 0.10)
                    ENDTMP(120, runTo.X, runTo.Y, runTo.Z);//Loc.X, Loc.Y, Loc.Z);
                return;
            }
            /*else if (lenX + lenY + lenZ < 3)
            {
                speed /= 2;
                return;
            }*/

            float speedX, speedY, speedZ;
            ushort count;

            if (lenX > lenY && lenX > lenZ)
            {
                count = (ushort)(lenX / speed);
                speedX = speed;
                speedY = speed * (lenY / lenX);
                speedZ = speed * (lenZ / lenX);
            }
            else if (lenY > lenX && lenY > lenZ)
            {
                count = (ushort)(lenY / speed);
                speedY = speed;
                speedX = speed * (lenX / lenY);
                speedZ = speed * (lenZ / lenY);
            }
            else
            {
                count = (ushort)(lenZ / speed);
                speedZ = speed;
                speedX = speed * (lenX / lenZ);
                speedY = speed * (lenY / lenZ);
            }
            if (wayX < 0)
                speedX = -speedX;
            if (wayY < 0)
                speedY = -speedY;
            if (wayZ < 0)
                speedZ = -speedZ;

            //xxxxxx
            ushort n_final = (ushort)(n + count);
            float moveX = Location.X, moveY = Location.Y, moveZ = Location.Z;
            //for (; ti < ti_final; ti++)
            {
                //Откат =(
                /*if (Loc.X != moveX || Loc.Y != moveY && Loc.Z != moveZ)
                {
                    System.Threading.Thread.Sleep(500);
                    ENDTMP(120, Loc.X, Loc.Y, Loc.Z);
                    System.Threading.Thread.Sleep(500);
                    TMP(toX, toY, toZ, t);
                    return;
                }*/

                moveX += speedX; //LocX - (((ti_final - ti - countX) + 1) * speed);
                moveY += speedY;
                moveZ += speedZ;
                TMP_hlp(moveX, moveY, moveZ);
                Location.X = moveX;
                Location.Y = moveY;
                Location.Z = moveZ;
            }
            //ENDTMP(120, Loc.X, Loc.Y, Loc.Z);
        }
        public void TMP_hlp(float x, float y, float z)
        {
            p_Moving(new Point3(x, y, z), 33, 8 * 0x100);
        }
        public void ENDTMP(byte dir, float x = 0F, float y = 0F, float z = 0F)
        {
            p_EndMove(new Point3(x, y, z), 33, 8 * 0x100, 10);
        }
        public void Run(Point3 loc)
        {
            this.runFrom = Location.Clone();
            this.runTo = loc;
            runTimer.Enabled = true;
            runTimer.Start();
        }
        //НУБОКОД_КОНЕЦ!!!
        //@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!@!

        /// <summary>
        /// Берёт объект в цель
        /// </summary>
        /// <param name="id">Ид объекта</param>
        public void SelectTarget(uint id)
        {
            p_SelectTarget(id);
        }
        /// <summary>
        /// Использует скилл (кд не реализован, соблюдайте паузы!!!)
        /// </summary>
        /// <param name="mobId">Ид объекта</param>
        /// <param name="skillId">Ид скила</param>
        public void UseSkill(uint mobId, ushort skillId)
        {
            p_UseSkill(mobId, skillId);
        }
        /// <summary>
        /// Использует скилл (кд не реализован, соблюдайте паузы!!!)
        /// </summary>
        /// <param name="mobId">Ид моба</param>
        /// <param name="skillName">Имя скила (PWData)</param>
        public void UseSkill(uint mobId, string skillName)
        {
            UseSkill(mobId, PWData.Skills[skillName]);
        }

        /// <summary>
        /// Изображает эмоцию
        /// </summary>
        /// <param name="id">Ид эмоции</param>
        public void Emote(ushort id)
        {
            p_SPacket pkt = new p_SPacket(0x30);
            pkt.AddUInt16(id);
            sendAsync(pkt);
        }
        /// <summary>
        /// Изображает эмоцию
        /// </summary>
        /// <param name="emotion">Эмоция</param>
        public void Emote(PWData.Emote emotion)
        {
            Emote((ushort)emotion);
        }

        private ActionQueueAsync chatQueue;
        private string[] blockMsg(string str)
        {
            if (str.Length <= 72)
                return new string[] { str };

            List<string> result = new List<string>();
            for (int i = 0; i < str.Length; i += 72)
            {
                int length;
                if (str.Length - i < 72)
                    length = str.Length - i;
                else
                    length = 72;
                result.Add(str.Substring(i, length));
            }
            return result.ToArray();
        }
        /// <summary>
        /// Посылает сообщение персонажу
        /// </summary>
        /// <param name="nick">Ник персонажа</param>
        /// <param name="msg">Сообщение</param>
        /// <exception cref="EmptyStringException" />
        public void SendPrivateMessage(string nick, string msg)
        {
            if (nick.Length == 0 || msg.Length == 0)
                throw new EmptyStringException();

            var block = blockMsg(msg);
            foreach (var elm in block)
            {
                chatQueue.Start(() =>
                {
                    p_SendPrivateMessage(new ChatMessage { SenderNick = SelectedChar.Name, SenderUID = SelectedChar.Id, ReceiverNick = nick, ReceiverUID = 0, Message = elm });
                    System.Threading.Thread.Sleep(RandomUtil.Next(1200, 1500));
                });
            }
        }
        /// <summary>
        /// Посылает сообщение в локальный чат
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <exception cref="EmptyStringException" />
        public void SendLocalMessage(string msg)
        {
            if (msg.Length == 0)
                throw new EmptyStringException();
            var block = blockMsg(msg);
            foreach (var elm in block)
            {
                chatQueue.Start(() =>
                {
                    p_SendLocalMessage(new ChatMessage { SenderUID = SelectedChar.Id, Message = elm });
                    System.Threading.Thread.Sleep(RandomUtil.Next(1200, 1500));
                });
            }
        }

        /// <summary>
        /// Возрождает персонажа
        /// </summary>
        /// <param name="type">Тип возрождения</param>
        /// <exception cref="PersAliveException" />
        public void Ressurect(RessurectType type)
        {
            if (HP != 0)
                throw new PersAliveException();
            if (type == RessurectType.ToTown)
                p_RessurectToTown();
            else if (type == RessurectType.UseScroll)
            {
                //Проверка в инвентаре на скролл
                p_RessurectWithScroll();
            }
            else if (type == RessurectType.UseBuff)
            {
                //Проверка на бафф
                p_RessurectWithBuff();
            }
        }
        /// <summary>
        /// Поднимает навыки за очки распределения
        /// </summary>
        /// <param name="vitality">Выносливость</param>
        /// <param name="intelligence">Интеллект</param>
        /// <param name="strenght">Сила</param>
        /// <param name="agility">Ловкость</param>
        /// <exception cref="FewStatsException" />
        public void IncreaseStats(uint vitality, uint intelligence, uint strenght, uint agility)
        {
            uint sum = vitality + intelligence + strenght + agility;
            if (sum > FreeStats)
                throw new FewStatsException();
            FreeStats -= sum;
            p_IncreaseStats(vitality, intelligence, strenght, agility);
            p_ApplyStatsChange();
        }

        /// <summary>
        /// Выкидывает деньги
        /// </summary>
        /// <param name="count">Количество</param>
        public void DropGold(uint count)
        {
            if (Money - count < 0)
                throw new ArgumentException();
            p_DropMoney(count);
        }
        /// <summary>
        /// Закрыват окно торговли(кота)
        /// </summary>
        public void CloseTrade()
        {
            p_ClosePersonalMarket();
        }
    }
}
