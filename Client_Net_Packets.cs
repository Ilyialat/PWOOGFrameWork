using System;
using System.Collections.Generic;
using System.Threading;


namespace PWOOGFrameWork
{
    public partial class PWClient
    {
        private class PacketAttribute : Attribute
        {
            public ushort Header;
            public ushort Version;
            public bool IsFromContainer;

            private PacketAttribute() { }

            public PacketAttribute(ushort header, bool isFromContainer = true)
            {
                this.Header = header;
                this.IsFromContainer = isFromContainer;
            }
        }
        private Dictionary<PacketAttribute, PacketHandler> _p;
        private void p(PWStream pkt)
        {
            foreach (var elm in _p)
            {
                if (elm.Key.Header == pkt.Header && elm.Key.IsFromContainer == pkt.IsContainer)
                {
                    try { elm.Value(pkt); }
                    catch { }
                    return;
                }
            }
        }

        private delegate void PacketHandler(PWStream pkt);

        private void p_Inicialization()
        {
            _p = new Dictionary<PacketAttribute, PacketHandler>(ushort.MaxValue);

            var phType = typeof(PacketHandler);
            var paType = typeof(PacketAttribute);

            //Получение всех методов с атрибутом Packet и добавление их в словарь
            var funcArr = typeof(PWClient).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var func in funcArr)
            {
                var atrbArr = func.GetCustomAttributes(paType, false);
                if (atrbArr.Length == 1)
                {
                    var atrb = (PacketAttribute)atrbArr[0];
                    _p.Add(atrb, (PacketHandler)Delegate.CreateDelegate(phType, this, func));
                }
            }
        }


        //0x00
        private void p_Moving(Point3 loc, byte type, ushort speed)
        {
            p_SPacket pkt = new p_SPacket(0x0, true);
            pkt.AddSingle(loc.X);
            pkt.AddSingle(loc.Z);
            pkt.AddSingle(loc.Y);
            pkt.AddSingle(loc.X);
            pkt.AddSingle(loc.Z);
            pkt.AddSingle(loc.Y);
            pkt.AddUInt16(500); // Дельта времени(обычно 500)
            pkt.AddUInt16(speed); // Скорость
            pkt.AddByte(type); // Тип движения(Бег, полет и тд)
            pkt.AddUInt16(n++); // Номер пакета на движение
            sendAsync(pkt);
        }
        [Packet(0x01, false)]
        private void p_ServerInfo(PWStream pkt)
        {
            if (Key != null)
                return;
            byte KeyLen = pkt.ReadByte();
            Key = pkt.ReadByte(KeyLen);
            p_SendLogginAnnounce();
        }
        //0x02
        private void p_SendCMKey(byte[] decHash)
        {
            p_SPacket pkt = new p_SPacket(0x02, false);
            pkt.AddByte((byte)decHash.Length);
            pkt.AddByte(decHash);
            pkt.AddByte(Force);
            sendAsync(pkt);
        }
        //0x02
        private void p_SelectTarget(uint id)
        {
            p_SPacket pkt = new p_SPacket(0x02);
            pkt.AddUInt32(id);
            sendAsync(pkt);
        }
        [Packet(0x02, false)]
        private void p_SMKey(PWStream pkt)
        {
            byte[] decHash, RC4Encode, RC4Decode, encHash; byte encHashLen;
            encHashLen = pkt.ReadByte();
            encHash = pkt.ReadByte(encHashLen);
            Force = pkt.ReadByte();

            //Начинаем шифровку
            IsLoginCompleted = true;
            LoginResult = true;
            if (LoginCompleted != null)
                LoginCompleted(LoginResult);
            //Генерируем дек хэш
            decHash = MD5Hash.GetDecodeHash();
            //Делаем из полученного хэша ключ шифрования
            RC4Encode = MD5Hash.GetRC4Key(encHash, Login, Hash);
            //Делаем из сгенерированного хэша ключ расшифровки
            RC4Decode = MD5Hash.GetRC4Key(decHash, Login, Hash);
            //Создаем крипт
            crypt = new PWCrypt(RC4Encode, RC4Decode);
            //Посылаем хэш серверу
            p_SendCMKey(decHash);
        }
        //0x03
        private void p_SendLogginAnnounce()
        {
            Hash = MD5Hash.GetHash(Login, Password, Key);
            p_SPacket pkt = new p_SPacket(0x03, false);
            pkt.AddAString(Login);
            pkt.AddByte((byte)Hash.Length);
            pkt.AddByte(Hash);
            pkt.AddByte(new byte[] { 0x00, 0x04, 0xFF, 0xFF, 0xFF, 0xFF });
            sendAsync(pkt);
        }
        [Packet(0x04, false)]
        private void p_OnlineAnnounce(PWStream pkt)
        {
            AccountKey = pkt.ReadUInt32();
            uint unk = pkt.ReadUInt32();
            uint unk1 = pkt.ReadUInt32();
            byte unkLen = pkt.ReadByte();
            uint unk2 = pkt.ReadUInt32();
            uint slotId = pkt.ReadUInt32();
            //byte[] unkData = pkt.ReadByte(18);
            p_SendRoleList(slotId);
        }
        [Packet(0x04, true)]
        private void p_PlayerList(PWStream pkt)
        {
            ushort Count = pkt.ReadUInt16();
            for (byte i = 0; i < Count; i++)
            {
                uint id = pkt.ReadUInt32();
                float x = pkt.ReadSingle();
                float z = pkt.ReadSingle();
                float y = pkt.ReadSingle();
                ushort unk1 = pkt.ReadUInt16();
                ushort unk2 = pkt.ReadUInt16();
                byte angle = pkt.ReadByte();
                byte unk3 = pkt.ReadByte();

                uint mask = pkt.ReadUInt32();

                if (mask == 0)
                    pkt.ReadByte(4);
                if ((mask & 0x400) == 0x400)
                    pkt.ReadByte(8);
                if ((mask & 0x1) == 0x1)
                    pkt.ReadByte(5);
                if ((mask & 0x2) == 0x02)
                    pkt.ReadByte();
                if ((mask & 0x40) == 0x40)
                    pkt.ReadByte(16);
                if ((mask & 0x800) == 0x800)
                {
                    //pkt.ReadUInt32();
                    pkt.ReadByte();
                }
                if ((mask & 0x1000) == 0x1000)
                    pkt.ReadByte();
                if ((mask & 0x10000) == 0x10000)
                {
                    byte len = pkt.ReadByte();
                    pkt.ReadUString(len);
                }
                if ((mask & 0x8) == 0x08)
                    pkt.ReadByte();
                if ((mask & 0x8000) == 0x8000)
                    pkt.ReadByte(4);
                if ((mask & 0x80000) == 0x80000)
                    pkt.ReadByte(6);
                if ((mask & 0x100000) == 0x100000)
                    pkt.ReadByte(5);
                if ((mask & 0x800000) == 0x800000)
                    pkt.ReadUInt32();
                if ((mask & 0x20000000) == 0x20000000)
                    pkt.ReadUInt32();

                GameObjects[id] = new GameObject { Type = GameObjectType.Player, Location = new Point3(x, y, z) };
                continue;


                while (true)
                {
                    if (mask >= 0x20000000)
                    {
                        mask ^= 0x20000000;
                        pkt.ReadUInt32();
                    }
                    else if (mask >= 0x800000)
                    {
                        mask ^= 0x800000;
                        pkt.ReadUInt32();
                    }
                    else if (mask >= 100000)
                    {
                        mask ^= 100000;
                        pkt.ReadByte(5);
                    }
                    else if (mask >= 80000)
                    {
                        mask ^= 80000;
                        pkt.ReadByte(6);
                    }
                    else if (mask >= 0x10000)
                    {
                        mask ^= 10000;
                        byte len = pkt.ReadByte();
                        pkt.ReadUString(len);
                    }
                    else if (mask >= 0x8000)
                    {
                        mask ^= 8000;
                        pkt.ReadByte(4);
                    }
                    else if (mask >= 0x1000)
                    {
                        mask ^= 1000;
                        pkt.ReadByte();
                    }
                    else if (mask >= 0x800)
                    {
                        mask ^= 800;
                        pkt.ReadUInt32();
                        //pkt.ReadByte();
                    }
                    else if (mask >= 0x400)
                    {
                        mask ^= 400;
                        pkt.ReadByte(8);
                    }
                    else if (mask >= 0x40)
                    {
                        mask ^= 40;
                        pkt.ReadByte(16);
                    }
                    else if (mask >= 0x8)
                    {
                        mask ^= 0x8;
                        pkt.ReadByte();
                    }
                    else if (mask >= 0x2)
                    {
                        mask ^= 0x2;
                        pkt.ReadByte();
                    }
                    else if (mask >= 0x1)
                    {
                        mask ^= 0x1;
                        pkt.ReadByte();
                    }
                    else
                        break;
                }


                /*                    else if (mask == 0x400)
                    pkt.ReadByte(8);
                else if (mask == 0x1)
                    pkt.ReadByte();
                else if (mask == 0x02)
                    pkt.ReadByte();
                else if (mask == 0x40)
                    pkt.ReadByte(16);
                else if (mask == 0x300)
                    pkt.ReadByte(4);
                else if (mask == 0x800)
                {
                    pkt.ReadUInt32();
                    pkt.ReadByte();
                }
                else if (mask == 0x1000)
                    pkt.ReadByte();
                else if (mask == 0x10000)
                {
                    byte len = pkt.ReadByte();
                    pkt.ReadUString(len);
                }
                else if (mask == 0x08)
                    pkt.ReadByte();
                else if (mask == 0x8000)
                    pkt.ReadByte(4);
                else if (mask == 0x86B1)
                    pkt.ReadByte(4);
                else if (mask == 0x8841)
                    pkt.ReadByte(4);
                else if (mask == 0x80000)
                    pkt.ReadByte(6);
                else if (mask == 0x100000)
                    pkt.ReadByte(5);
                else if (mask == 0x800000)
                    pkt.ReadUInt32();
                else if (mask == 0x20000000)
                    pkt.ReadUInt32();
                else if (mask == 0x2000A801)
                    pkt.ReadUInt32();
                else if (mask == 0x2000A810)
                    pkt.ReadUInt32();
                else
                    pkt.ReadByte(4);*/

                /*if (mask == 0x0)
                    pkt.ReadByte(4);
                else if (mask == 0x400)
                    pkt.ReadByte(8);
                else if (mask == 0x1)
                    pkt.ReadByte();
                else if (mask == 0x02)
                    pkt.ReadByte();
                else if (mask == 0x40)
                    pkt.ReadByte(16);
                else if (mask == 0x300)
                    pkt.ReadByte(4);
                else if (mask == 0x800)
                {
                    pkt.ReadUInt32();
                    pkt.ReadByte();
                }
                else if (mask == 0x1000)
                    pkt.ReadByte();
                else if (mask == 0x10000)
                {
                    byte len = pkt.ReadByte();
                    pkt.ReadUString(len);
                }
                else if (mask == 0x08)
                    pkt.ReadByte();
                else if (mask == 0x8000)
                    pkt.ReadByte(4);
                else if (mask == 0x86B1)
                    pkt.ReadByte(4);
                else if (mask == 0x8841)
                    pkt.ReadByte(4);
                else if (mask == 0x80000)
                    pkt.ReadByte(6);
                else if (mask == 0x100000)
                    pkt.ReadByte(5);
                else if (mask == 0x800000)
                    pkt.ReadUInt32();
                else if (mask == 0x20000000)
                    pkt.ReadUInt32();
                else if (mask == 0x2000A801)
                    pkt.ReadUInt32();
                else if (mask == 0x2000A810)
                    pkt.ReadUInt32();
                else
                    pkt.ReadByte(4);*/
            }
        }
        [Packet(0x05, false)]
        private void p_ErrorInfo(PWStream pkt)
        {
            IsLoginCompleted = true;
            LoginResult = false;
            if (LoginCompleted != null)
                LoginCompleted(LoginResult);
            byte errorCode = pkt.ReadByte();
            byte messageLen = pkt.ReadByte();
            string message = pkt.ReadAString(messageLen);
            //Console.WriteLine(message);
        }
        //0x07
        private void p_EndMove(Point3 loc, byte type, ushort speed, byte dir)
        {
            /*
<PacketInfo Type="0x07" Direction="C2S" Container="True" Name="EndMove">
<PacketField Type="Float" Name="X" />
<PacketField Type="Float" Name="Z" />
<PacketField Type="Float" Name="Y" />
<PacketField Type="Word" Name="Speed" />
<PacketField Type="Byte" Name="Dir" />
<PacketField Type="Byte" Name="Mode" />
<PacketField Type="Word" Name="MoveSeq" />
<PacketField Type="Word" Name="msec" />
</PacketInfo>
* */

            p_SPacket pkt = new p_SPacket(0x07);
            pkt.AddSingle(loc.X);
            pkt.AddSingle(loc.Z);
            pkt.AddSingle(loc.Y);

            pkt.AddUInt16(speed); // Скорость
            pkt.AddByte(dir); // Угол поворота
            pkt.AddByte(type); // Тип движения(Бег, полет и тд)
            pkt.AddUInt16(n++); // Номер пакета на движение
            pkt.AddUInt16(500); // Дельта времени(обычно 500)
            sendAsync(pkt);
        }
        [Packet(0x08)]
        private void p_PlayerPos(PWStream pkt)
        {
            /*
<PacketInfo Type="0x08" Direction="S2C" Container="True" Name="PlayerPos">
<PacketField Type="Dword" Name="Exp" />
<PacketField Type="Dword" Name="Spirit" />
<PacketField Type="Dword" Name="MyUID" />
<PacketField Type="Float" Name="X" />
<PacketField Type="Float" Name="Z" />
<PacketField Type="Float" Name="Y" />
</PacketInfo>
*/

            uint exp = pkt.ReadUInt32();
            uint spirit = pkt.ReadUInt32();
            uint id = pkt.ReadUInt32();
            float x = pkt.ReadSingle();
            float z = pkt.ReadSingle();
            float y = pkt.ReadSingle();

            if (id == SelectedChar.Id)
            {
                this.Location = new Point3(x, y, z);
                this.Expirience = exp;
                this.Spirit = spirit;
            }



            /*var obj = GameObjects[id];
            GameObjects[id] = obj;*/
        }
        [Packet(0x09)]
        private void p_MobList(PWStream pkt)
        {
            byte Count = pkt.ReadByte();
            for (byte i = 0; i < Count; i++)
            {
                byte unk1 = pkt.ReadByte();
                uint id = pkt.ReadUInt32();
                uint UnkID = pkt.ReadUInt32();
                float x = pkt.ReadSingle();
                float z = pkt.ReadSingle();
                float y = pkt.ReadSingle();
                byte[] unk2 = pkt.ReadByte(6);

                var obj = new GameObject();
                obj.Type = GameObjectType.Mob;
                obj.Location = new Point3(x, y, z);
                GameObjects[id] = obj;
            }
        }

        //0x0C
        private void p_SwapItem(byte inventorySlot1, byte inventorySlot2)
        {
            /*<PacketInfo Type="0x0C" Direction="C2S" Container="True" Name="ItemMove">
  <PacketField Type="Byte" Name="cellstart" />
  <PacketField Type="Byte" Name="cellend" />
</PacketInfo>*/
            p_SPacket pkt = new p_SPacket(0x0C);
            pkt.AddByte(inventorySlot1);
            pkt.AddByte(inventorySlot2);
            sendAsync(pkt);
        }
        //0x0D
        private void p_SplitItem(byte inventorySlot1, byte inventorySlot2, ushort count)
        {
            p_SPacket pkt = new p_SPacket(0x0D);
            pkt.AddByte(inventorySlot1);
            pkt.AddByte(inventorySlot2);
            pkt.AddUInt16(count);
            sendAsync(pkt);
            /*
      <PacketInfo Type="0x0D" Direction="C2S" Container="True" Name="ItemsMove(part)">
        <PacketField Type="Byte" Name="cellstart" />
        <PacketField Type="Byte" Name="cellend" />
        <PacketField Type="Word" Name="count" />
      </PacketInfo>
             * */
        }
        [Packet(0x0D)]
        private void p_PlayerBreakRadius(PWStream pkt)
        {
            uint uid = pkt.ReadUInt32();
            GameObjects.Remove(uid);
        }

        [Packet(0x0F)]
        private void p_ObjectMove(PWStream pkt)
        {
            /*<PacketInfo Type="0x0F" Direction="S2C" Container="True" Name="ObjectMove">
  <PacketField Type="Dword" Name="UID" />
  <PacketField Type="Float" Name="X" />
  <PacketField Type="Float" Name="Z" />
  <PacketField Type="Float" Name="Y" />
</PacketInfo>
       */

            uint id = pkt.ReadUInt32();
            float x = pkt.ReadSingle();
            float z = pkt.ReadSingle();
            float y = pkt.ReadSingle();

            var obj = GameObjects[id];
            obj.IsMoving = true;
            obj.Location = new Point3(x, y, z);
            GameObjects[id] = obj;
        }

        //0x14
        private void p_DropMoney(uint count)
        {
            /*<PacketInfo Type="0x14" Direction="C2S" Container="True" Name="DropMoney">
  <PacketField Type="Dword" Name="Count" />
</PacketInfo>*/
            p_SPacket pkt = new p_SPacket(0x14);
            pkt.AddUInt32(count);
            sendAsync(pkt);
        }

        [Packet(0x20)]
        private void p_TargetInfo(PWStream pkt)
        {
            uint uid = pkt.ReadUInt32();
            ushort lvl = pkt.ReadUInt16();
            pkt.ReadByte(); //unk
            pkt.ReadByte(); //unk
            uint hp = pkt.ReadUInt32();
            uint maxHp = pkt.ReadUInt32();
            uint mp = pkt.ReadUInt32();
            uint maxMp = pkt.ReadUInt32();

            GameObject obj;
            if (!GameObjects.TryGetValue(uid, out obj))
                return; //Приходят неизвестные id-ы, ну их
            obj.HP = hp;
            obj.MaxHP = maxHp;
            obj.MP = mp;
            obj.MaxMP = maxMp;
            obj.Level = lvl;
            GameObjects[uid] = obj;

            if (TargetInfo != null)
                TargetInfo(obj);
        }
        [Packet(0x21)]
        private void p_ObjectHP(PWStream pkt)
        {
            uint uid = pkt.ReadUInt32();
            uint hp = pkt.ReadUInt32();
            uint maxHp = pkt.ReadUInt32();

            var obj = GameObjects[uid];
            obj.HP = hp;
            obj.MaxHP = maxHp;
            GameObjects[uid] = obj;

            if (TargetInfo != null)
                TargetInfo(obj);
        }
        [Packet(0x22)]
        private void p_ObjectRemove(PWStream pkt)
        {
            pkt.ReadUInt32(); //unk
            uint uid = pkt.ReadUInt32();
            GameObjects.Remove(uid);
        }

        [Packet(0x23)]
        private void p_ObjectMoveStop(PWStream pkt)
        {
            /*
<PacketInfo Type="0x23" Direction="S2C" Container="True" Name="ObjectMoveStop">
<PacketField Type="Dword" Name="UID" />
<PacketField Type="Float" Name="X" />
<PacketField Type="Float" Name="Z" />
<PacketField Type="Float" Name="Y" />
</PacketInfo>*/
            uint id = pkt.ReadUInt32();
            float x = pkt.ReadSingle();
            float z = pkt.ReadSingle();
            float y = pkt.ReadSingle();

            var obj = GameObjects[id];
            obj.Location = new Point3(x, y, z);
            obj.IsMoving = false;
            GameObjects[id] = obj;
        }

        //!!!!!!!!
        [Packet(0x26)]
        private void p_PlayerMainInfo(PWStream pkt)
        {
            ushort Level = pkt.ReadUInt16();
            ushort unk1 = pkt.ReadUInt16();
            uint HP = pkt.ReadUInt32();
            uint MaxHP = pkt.ReadUInt32();
            uint MP = pkt.ReadUInt32();
            uint MaxMP = pkt.ReadUInt32();
            uint Exp = pkt.ReadUInt32();
            uint Spirit = pkt.ReadUInt32();
            uint unk2 = pkt.ReadUInt32();
            uint unk3 = pkt.ReadUInt32();

            this.Level = Level;
            this.HP = HP;
            this.MaxHP = MaxHP;
            this.MP = MP;
            this.MaxMP = MaxMP;
            this.Expirience = Exp;
            this.Spirit = Spirit;
        }
        //0x28
        private void p_UseItem(ushort slot, uint id)
        {
            /*
             *   <PacketInfo Type="0x28" Direction="C2S" Container="True" Name="UseItem">
        <PacketField Type="Byte" Name="InventoryID" />
        <PacketField Type="Byte" Name="Unk" />
        <PacketField Type="Bytes" Length="2" Name="Unk" />
        <PacketField Type="Dword" Name="ItemID" />
        </PacketInfo>
             * */
            p_SPacket pkt = new p_SPacket(0x28);
            pkt.AddByte(new byte[] { 00, 01 }); //type
            pkt.AddUInt16(slot);
            pkt.AddUInt32(id);
            sendAsync(pkt);
        }
        //0x29
        private void p_UseSkill(uint targetId, uint skillId)
        {
            p_SPacket pkt = new p_SPacket(0x29);
            pkt.AddUInt32(skillId);
            pkt.AddByte(new byte[] { 0x00, 0x01 });
            pkt.AddUInt32(targetId);
            sendAsync(pkt);
        }

        [Packet(0x36)]
        private void p_ObjectMoveBack(PWStream pkt)
        {
            uint id = pkt.ReadUInt32();
            if (id != SelectedChar.Id)
                return;
            //float float float float

            runTimer.Stop();
            //System.Windows.Forms.MessageBox.Show("Otkat =(");
        }

        [Packet(0xB1)]
        private void p_ObjectMoveBack_Cord(PWStream pkt)
        {
            float x = pkt.ReadSingle();
            float z = pkt.ReadSingle();
            float y = pkt.ReadSingle();
            ushort n = pkt.ReadUInt16();

            Location = new Point3(x, y, z);
            this.n = n;
        }
        /*<PacketInfo Type="0x04" Direction="S2C" Container="True" Name="PlayerList">
          <PacketField Type="Word" Name="Count" />
          <PacketDataBlock Count="Count.Value" Name="Player">
            <PacketField Type="Dword" Name="ID" />
            <PacketField Type="Float" Name="X" />
            <PacketField Type="Float" Name="Z" />
            <PacketField Type="Float" Name="Y" />
            <PacketField Type="Word" Name="Unk" />
            <PacketField Type="Word" Name="Unk" />
            <PacketField Type="Byte" Name="Angle" />
            <PacketField Type="Byte" Name="Unk" />
            <PacketField Type="Dword" Name="Mask" />
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x400" Desc="0x400">
              <PacketField Type="Bytes" Length="8" Name="0x400" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x1" Desc="0x1">
              <PacketField Type="Byte" Name="0x1" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x2" Desc="0x2">
              <PacketField Type="Byte" Name="0x2" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x40" Desc="0x40">
              <PacketField Type="Bytes" Length="16" Name="0x40" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x800" Desc="0x800">
              <PacketField Type="Dword" Name="Guild ID" />
              <PacketField Type="Byte" Name="Clan status" Desc="Должность в клане 2-Мастер .. 6-член" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x1000" Desc="0x1000">
              <PacketField Type="Byte" Name="0x1000" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x10000" Desc="0x10000">
              <PacketField Type="Byte" Name="Length" />
              <PacketField Type="UString" Length="Length.Value" Name="Dat" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x8" Desc="0x8">
              <PacketField Type="Byte" Name="0x8" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x80000" Desc="0x80000">
              <PacketField Type="Bytes" Length="6" Name="0x80000" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x100000" Desc="0x100000">
              <PacketField Type="Bytes" Length="5" Name="0x100000" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x800000" Desc="0x800000">
              <PacketField Type="Dword" Name="0x800000" />
            </If>
            <If Clause="MaskAny" Value1="Mask.Value" Value2="0x20000000" Desc="0x20000000">
              <PacketField Type="Dword" Name="0x20000000" />
            </If>
          </PacketDataBlock>
        </PacketInfo>*/
        /*<PacketInfo Type="0x0C" Direction="S2C" Container="True" Name="PlayerEnterRadius">
          <PacketField Type="Dword" Name="ID" />
          <PacketField Type="Float" Name="X" />
          <PacketField Type="Float" Name="Z" />
          <PacketField Type="Float" Name="Y" />
          <PacketField Type="Word" Name="Unk" />
          <PacketField Type="Word" Name="Unk" />
          <PacketField Type="Byte" Name="Angle" />
          <PacketField Type="Byte" Name="Unk" />
          <PacketField Type="Dword" Name="Mask" />
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x400" Desc="0x400">
            <PacketField Type="Bytes" Length="8" Name="0x400" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x1" Desc="0x1">
            <PacketField Type="Byte" Name="0x1" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x2" Desc="0x2">
            <PacketField Type="Byte" Name="0x2" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x40" Desc="0x40">
            <PacketField Type="Bytes" Length="16" Name="0x40" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x800" Desc="0x800">
            <PacketField Type="Dword" Name="Guild ID" />
            <PacketField Type="Byte" Name="Clan status" Desc="Должность в клане 2-Мастер .. 6-член" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x1000" Desc="0x1000">
            <PacketField Type="Byte" Name="0x1000" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x10000" Desc="0x10000">
            <PacketField Type="Byte" Name="Length" />
            <PacketField Type="UString" Length="Length.Value" Name="Dat" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x8" Desc="0x8">
            <PacketField Type="Byte" Name="0x8" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x80000" Desc="0x80000">
            <PacketField Type="Bytes" Length="6" Name="0x80000" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x100000" Desc="0x100000">
            <PacketField Type="Bytes" Length="5" Name="0x100000" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x800000" Desc="0x800000">
            <PacketField Type="Dword" Name="0x800000" />
          </If>
          <If Clause="MaskAny" Value1="Mask.Value" Value2="0x20000000" Desc="0x20000000">
            <PacketField Type="Dword" Name="0x20000000" />
          </If>
        </PacketInfo>
        <PacketInfo Type="0x0D" Direction="S2C" Container="True" Name="PlayerBreakRadius">
          <PacketField Type="Dword" Name="UID" />
        </PacketInfo>*/

        //krukovis_start
        private void p_StartMeditation()
        {
            p_SPacket pkt = new p_SPacket(0x2E);
            sendAsync(pkt);
        }
        private void p_StopMeditation()
        {
            p_SPacket pkt = new p_SPacket(0x2F);
            sendAsync(pkt);
        }
        private void p_UseFly(uint flyId)
        {
            p_SPacket pkt = new p_SPacket(0x28);
            pkt.AddByte(new byte[] { 0x1, 0x1, 0xC, 0x0 });
            pkt.AddUInt32(flyId);
            sendAsync(pkt);
        }
        private void p_FastFly(uint flag)
        {
            p_SPacket pkt = new p_SPacket(0x5A);
            pkt.AddUInt32(flag); //1 0 0 0 
            sendAsync(pkt);
        }
        private void p_PlayerAttack()
        {
            p_SPacket pkt = new p_SPacket(0x3);
            pkt.AddByte(0);
            sendAsync(pkt);
        }
        private void p_RessurectToTown()
        {
            p_SPacket pkt = new p_SPacket(0x4);
            sendAsync(pkt);
        }
        private void p_RessurectWithScroll()
        {
            p_SPacket pkt = new p_SPacket(0x5);
            sendAsync(pkt);
        }
        private void p_BeIntimate()
        {
            p_SPacket pkt = new p_SPacket(0x30);
            pkt.AddUInt16(0x1D);
            sendAsync(pkt);
        }
        private void p_RessurectWithBuff()
        {
            p_SPacket pkt = new p_SPacket(0x57);
            sendAsync(pkt);
        }
        private void p_ApplyStatsChange()
        {
            p_SPacket pkt = new p_SPacket(0x15);
            sendAsync(pkt);
        }
        private void p_IncreaseStats(uint vitality, uint intelligence, uint strenght, uint agility)
        {
            p_SPacket pkt = new p_SPacket(0x16);
            pkt.AddUInt32(vitality);
            pkt.AddUInt32(intelligence);
            pkt.AddUInt32(strenght);
            pkt.AddUInt32(agility);
            sendAsync(pkt);
        }
        private void p_DeselectTarget()
        {
            p_SPacket pkt = new p_SPacket(0x8);
            sendAsync(pkt);
        }

        /// <summary>
        /// 0-security 1-agressive 2-passive
        /// </summary>
        private void p_SetPetState(uint state)
        {
            p_SPacket pkt = new p_SPacket(0x67);
            pkt.AddUInt32(0);
            pkt.AddUInt32(3);
            pkt.AddUInt32(state);
            sendAsync(pkt);
        }

        private void p_CallPett(uint n)
        {
            p_SPacket pkt = new p_SPacket(0x64);
            pkt.AddUInt32(n);
            sendAsync(pkt);

        }
        private void p_HidePet()
        {
            p_SPacket pkt = new p_SPacket(0x65);
            sendAsync(pkt);
        }

        /// <summary>
        /// Если скил 0 - обычная атака
        /// </summary>
        private void p_PetUseSkill(uint targetId, uint skillId)
        {
            p_SPacket pkt = new p_SPacket(0x67);
            pkt.AddUInt32(targetId);

            if (skillId == 0)
            {
                pkt.AddUInt32(1);
            }
            else
            {
                pkt.AddUInt32(4);
                pkt.AddUInt32(skillId);
            }
            pkt.AddByte(0); //В старой версии + ещё 0
            sendAsync(pkt);
        }
        /// <summary>
        /// 0-follow 1-stop
        /// </summary>
        private void p_PetSetFollowState(uint flag)
        {
            p_SPacket pkt = new p_SPacket(0x67);
            pkt.AddUInt32(0); //targ
            pkt.AddUInt32(2); //flag
            pkt.AddUInt32(flag); //followflag
            sendAsync(pkt);
        }

        private void PickUpItem(uint itemWID, uint itemId)
        {
            p_SPacket pkt = new p_SPacket(0x6);
            pkt.AddUInt32(itemWID);
            pkt.AddUInt32(itemId);
            sendAsync(pkt);
        }
        private void MineResource(uint resourceWID)
        {
            p_SPacket pkt = new p_SPacket(0x36);
            pkt.AddUInt32(resourceWID); //тут был 0
            pkt.AddUInt16(0);
            pkt.AddUInt32(0); //тут было 201392158
            pkt.AddUInt32(0);
            pkt.AddUInt16(0);
            sendAsync(pkt);
        }
        //krukovis_end

        /*

         * */




        /*
  <PacketInfo Type="0x20" Direction="S2C" Container="True" Name="TargetInfo">
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="Word" Name="Level" />
    <PacketField Type="Byte" Name="Unk" />
    <PacketField Type="Byte" Name="Unk" />
    <PacketField Type="Dword" Name="HP" />
    <PacketField Type="Dword" Name="MaxHP" />
    <PacketField Type="Dword" Name="MP" />
    <PacketField Type="Dword" Name="MaxMP" />
  </PacketInfo>
  <PacketInfo Type="0x25" Direction="C2S" Container="True" Name="NPCContainer">
    <PacketField Type="Dword" Name="PacketType" />
    <PacketField Type="Dword" Name="PacketLen" />
    <If Clause="Equal" Value1="PacketType.Value" Value2="0x19">
      <PacketField Type="Dword" Name="unk" />
      <PacketField Type="Dword" Swap="True" Name="MyUID" />
      <PacketField Type="Dword" Name="unk" />
      <PacketField Type="Dword" Swap="True" Name="RecvUID" />
      <PacketField Type="CompactUint" Name="MessageNameLen" />
      <PacketField Type="UString" Length="MessageNameLen.Value" Name="MessageName" />
      <PacketField Type="CompactUint" Name="MessageLen" />
      <PacketField Type="UString" Length="MessageLen.Value" Name="Message" />
      <PacketField Type="Dword" Name="ItemID" />
      <PacketField Type="Dword" Name="ItemsCount" />
      <PacketField Type="Dword" Name="ItemSlot" />
      <PacketField Type="Dword" Name="Money" />
      <PacketField Type="Word" Name="unk" />
    </If>
  </PacketInfo>
         * */
        /*
  <PacketInfo Type="0x2A" Direction="S2C" Container="True" Name="QuestInventory">
    <PacketField Type="Byte" Name="InventoryType" />
    <PacketField Type="Byte" Name="Unk" />
    <PacketField Type="Dword" Name="DataLength" />
    <PacketDataBlock Count="32" Name="Items">
      <PacketField Type="Dword" Name="ID" />
      <If Clause="NotEqual" Value1="ID.Value" Value2="0xFFFFFFFF">
        <PacketField Type="Dword" Name="Unk1" />
        <PacketField Type="Dword" Name="Unk2" />
      </If>
    </PacketDataBlock>
  </PacketInfo>
         * */

        //0x27
        private void p_GetInventory()
        {
            Inventory.IsReceived = false;

            p_SPacket pkt = new p_SPacket(0x27);
            pkt.AddByte(1);
            pkt.AddByte(1);
            pkt.AddByte(0);
            sendAsync(pkt);
        }
        [Packet(0x2B)]
        private void p_Inventory(PWStream pkt)
        {
            /*
             *   <PacketInfo Type="0x2B" Direction="S2C" Container="True" Name="Inventory">
        <PacketField Type="Byte" Name="InventoryType" />
        <PacketField Type="Byte" Name="Unk" />
        <PacketField Type="Dword" Name="DataLength" />
        <PacketField Type="Dword" Name="ItemsCount" />
        <If Clause="NotEqual" Value1="ItemsCount.Value" Value2="0">
          <PacketDataBlock Count="ItemsCount.Value" Name="Items">
            <PacketField Type="Dword" Name="Slot" />
            <PacketField Type="Dword" Name="ID" />
            <PacketField Type="Dword" Name="Unk" />
            <PacketField Type="Dword" Name="Unk" />
            <PacketField Type="Dword" Name="ItemsInStack" />
            <PacketField Type="Word" Name="Unk" />
            <PacketField Type="Word" Name="ExtraDataLength" />
            <PacketField Type="Bytes" Length="ExtraDataLength.Value" Name="ExtraData" />
          </PacketDataBlock>
        </If>
      </PacketInfo>
             * */
            byte type = pkt.ReadByte();
            if (type != 0)
                return;
            byte capacity = pkt.ReadByte();
            pkt.ReadUInt32(); //data length
            uint count = pkt.ReadUInt32();

            Inventory.SetCapacity(capacity);

            var result = new InventoryItem[count];
            for (int i = 0; i < count; i++)
            {
                uint slot = pkt.ReadUInt32();
                uint id = pkt.ReadUInt32();
                pkt.ReadUInt32(); //unk
                pkt.ReadUInt32(); //unk
                uint itemCount = pkt.ReadUInt32();
                pkt.ReadUInt16(); //unk
                ushort extraLength = pkt.ReadUInt16();
                pkt.ReadByte(extraLength);
                Inventory.Insert((byte)slot, new InventoryItem { Type = type, Slot = (byte)slot, Id = id, Count = (ushort)itemCount });
            }
            Inventory.IsReceived = true;
        }

        //!!!!!
        [Packet(0x32)]
        private void p_PlayerFullInfo(PWStream pkt)
        {
            uint freeStats = pkt.ReadUInt32();
            pkt.ReadUInt32();
            pkt.ReadUInt32();
            uint criticalChance = pkt.ReadUInt32();
            uint spirit = pkt.ReadUInt32();
            uint invisible = pkt.ReadUInt32();
            uint trueSight = pkt.ReadUInt32();
            uint monsterDamage = pkt.ReadUInt32();
            uint monsterDefence = pkt.ReadUInt32();
            uint vitality = pkt.ReadUInt32();
            uint intellect = pkt.ReadUInt32();
            uint strenght = pkt.ReadUInt32();
            uint agility = pkt.ReadUInt32();
            uint maxHP = pkt.ReadUInt32();
            uint maxMP = pkt.ReadUInt32();
            pkt.ReadUInt32();
            pkt.ReadUInt32();
            uint criticalDamage = pkt.ReadUInt32();
            uint speed = pkt.ReadUInt32();
            var unk1 = pkt.ReadUInt32();
            var unk2 = pkt.ReadUInt32();
            uint accuracy = pkt.ReadUInt32();
            uint physicalAttackMin = pkt.ReadUInt32();
            uint physicalAttackMax = pkt.ReadUInt32();
            uint unk3 = pkt.ReadUInt32();
            uint unk4 = pkt.ReadUInt32();
            var unk5 = pkt.ReadByte(40);
            uint magicAttackMin = pkt.ReadUInt32();
            uint magicAttackMax = pkt.ReadUInt32();
            uint metalDefence = pkt.ReadUInt32();
            uint treeDefence = pkt.ReadUInt32();
            uint waterDefence = pkt.ReadUInt32();
            uint fireDefence = pkt.ReadUInt32();
            uint landDefence = pkt.ReadUInt32();
            uint evasion = pkt.ReadUInt32();
            uint magicDefence = pkt.ReadUInt32();

            this.FreeStats = freeStats;
            this.CriticalChance = criticalChance;
            this.Speed = spirit;
            this.Invisible = invisible;
            this.TrueSight = trueSight;
            this.MonsterDamage = monsterDamage;
            this.MonsterDefence = monsterDefence;
            this.Vitality = vitality;
            this.Intellect = intellect;
            this.Strenght = strenght;
            this.Agility = agility;
            this.MaxHP = maxHP;
            this.MaxMP = maxMP;
            this.CriticalDamage = criticalDamage;
            this.Speed = speed;
            this.Accuracy = accuracy;
            this.PhysicalAttackMin = physicalAttackMin;
            this.PhysicalAttackMax = physicalAttackMax;
            this.MagicAttackMin = magicAttackMin;
            this.MagicAttackMax = magicAttackMax;
            this.MetalDefence = metalDefence;
            this.TreeDefence = treeDefence;
            this.WaterDefence = waterDefence;
            this.FireDefence = fireDefence;
            this.LandDefence = landDefence;
            this.Evasion = evasion;
            this.MagicDefence = magicDefence;
        }
        [Packet(0x34)]
        private void p_AutoTarget(PWStream pkt)
        {
            /*
  <PacketInfo Type="0x34" Direction="S2C" Container="True" Name="GoodTarget">
    <PacketField Type="Dword" Name="UID" />
  </PacketInfo>*/
            uint target = pkt.ReadUInt32();
            if (AutoTarget != null)
                AutoTarget(target);
        }
        /*
          <PacketInfo Type="0x42" Direction="S2C" Container="True" Name="PlayersEquipList" />
                 * */
        private void p_SendSelectRole()
        {
            p_SPacket pkt = new p_SPacket(0x46, false);
            pkt.AddUInt32(SelectedChar.Id);
            //!!!!!!! NEW VERSION!
            pkt.AddByte(0);
            //
            sendAsync(pkt);
        }
        [Packet(0x47, false)]
        private void p_SelectRole_Re(PWStream pkt)
        {
            byte[] Data = pkt.ReadByte(5);
            p_SendEnterWorld();
        }
        //0x48
        private void p_SendEnterWorld()
        {
            p_SPacket pkt = new p_SPacket(0x48, false);
            pkt.AddUInt32(SelectedChar.Id);
            pkt.AddByte(new byte[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 });
            sendAsync(pkt);
            //IsInGame
        }
        //0x4C
        private void p_OpenPersonalMarket(string name, TradeTerm[] items)
        {
            p_SPacket pkt = new p_SPacket(0x4C);
            /*
/*
<PacketInfo Type="0x4C" Direction="C2S" Container="True" Name="OpenPersonalMarket">
<PacketField Type="Word" Name="TotalItemsInMarket" />
<PacketField Type="UStringZ" Length="62" Name="MarketName" />
<PacketDataBlock Count="TotalItemsInMarket.Value" Name="MarketItem">
<PacketField Type="Dword" Name="ItemID" />
<PacketField Type="Dword" Name="SlotID" />
<PacketField Type="Dword" Name="Count" />
<PacketField Type="Dword" Name="Cost" />
</PacketDataBlock>
</PacketInfo>*/
            pkt.AddUInt16((ushort)items.Length);
            pkt.AddFullUString(name);
            for (uint i = 0; i < items.Length; i++)
            {
                var item = items[i];
                pkt.AddUInt32(item.Item.Id);
                pkt.AddUInt32((uint)item.Item.Slot);
                pkt.AddUInt32(item.Count);
                pkt.AddUInt32(item.Price);
            }
            sendAsync(pkt);
        }
        //4D
        private void p_ClosePersonalMarket()
        {
            p_SPacket pkt = new p_SPacket(0x4D);
            sendAsync(pkt);
        }
        //0x4F
        private void p_SendLocalMessage(ChatMessage msg)
        {
            p_SPacket pkt = new p_SPacket(0x4F, false);
            pkt.AddUInt16(85);//1877);

            pkt.AddUInt32(msg.SenderUID);
            pkt.AddUInt16(0);
            pkt.AddUInt16(0);
            pkt.AddUString(msg.Message);
            pkt.AddByte(0);
            sendAsync(pkt);

            /*<PacketInfo Type="0x4F" Direction="C2S" Container="False" Name="SendLocalMessage">
  <PacketField Type="Word" Name="unk" />
  <PacketField Type="Dword" Name="UID" />
  <PacketField Type="Word" Name="unk" />
  <PacketField Type="Word" Name="unk" />
  <PacketField Type="Byte" Name="MessageLen" />
  <PacketField Type="UString" Length="MessageLen.Value" Name="Message" />
  <PacketField Type="Byte" Name="Unk" />
</PacketInfo>
       * */
        }

        //0x52
        private void p_SendRoleList(uint slotId)
        {
            p_SPacket pkt = new p_SPacket(0x52, false);
            pkt.AddUInt32(AccountKey);
            pkt.AddUInt32(0);
            pkt.AddUInt32(slotId);
            sendAsync(pkt);
        }
        [Packet(0x52)]
        private void p_PlayerMoney(PWStream pkt)
        {
            /*
<PacketInfo Type="0x52" Direction="S2C" Container="True" Name="GetOwnMoney">
<PacketField Type="Dword" Name="Money" />
<PacketField Type="Dword" Name="MaxMoney" />
</PacketInfo>
 */
            this.Money = pkt.ReadUInt32();
            pkt.ReadUInt32(); //maxMoney
        }

        [Packet(0x53, false)]
        private void p_RoleList_Re(PWStream pkt)
        {
            uint unk1 = pkt.ReadUInt32();
            uint nexSlotId = pkt.ReadUInt32();
            uint accountKey = pkt.ReadUInt32();
            uint unk2 = pkt.ReadUInt32();
            byte isChar = pkt.ReadByte();
            if (isChar != 0x01)
            {
                IsCharsReceived = true;
                if (CharsReceived != null)
                    CharsReceived();
                KeepAliveTimer = new System.Timers.Timer();
                KeepAliveTimer.Interval = 15000 + new Random().Next(-2000, +2000);
                KeepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler((object obj, System.Timers.ElapsedEventArgs e) => { p_SendKeepAlive(); });
                KeepAliveTimer.Start();
                return;
            }
            uint charId = pkt.ReadUInt32();
            byte gender = pkt.ReadByte();
            byte race = pkt.ReadByte();
            byte occupation = pkt.ReadByte();
            uint level = pkt.ReadUInt32();
            uint unk3 = pkt.ReadUInt32();
            byte nameLen = pkt.ReadByte();
            string name = pkt.ReadUString(nameLen);
            Chars.Add(new Char { Id = charId, Gender = gender, Race = race, Occupation = occupation, Level = level, Name = name });
            p_SendRoleList(nexSlotId);
        }
        //0x54
        private void p_OpenMarketWindow()
        {
            //<PacketInfo Type="0x54" Direction="C2S" Container="True" Name="OpenCatWindow" />
            p_SPacket pkt = new p_SPacket(0x54);
            sendAsync(pkt);
        }
        [Packet(0x5A)]
        private void p_KeepAlive(PWStream pkt)
        {
            byte unk = pkt.ReadByte();
        }
        //0x5A
        private void p_SendKeepAlive()
        {
            //KeepAliveTimer.Interval = 15000 + new Random().Next(-2000, +2000);
            p_SPacket pkt = new p_SPacket(0x5A, false);
            pkt.AddByte(90);
            sendAsync(pkt);
        }
        /*
  <PacketInfo Type="0x5B" Direction="C2S" Container="False" Name="GetPlayerAppearance">
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Bytes" Length="4" Name="Unk" />
    <PacketField Type="CompactUint" Name="Count" />
    <PacketDataBlock Count="Count.Value" Name="Players">
      <PacketField Type="Dword" Name="UID" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x5C" Direction="S2C" Container="False" Name="PlayerAppearance">
    <PacketField Type="Dword" Name="Unk" />
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Dword" Name="S04ID" />
    <PacketField Type="Byte" Name="Unk" />
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="Byte" Name="NickLength" />
    <PacketField Type="UString" Length="NickLength.Value" Name="Nick" />
    <PacketField Type="Dword" Name="Unk" />
    <PacketField Type="Dword" Name="Race" />
    <PacketField Type="Byte" Name="Gender" />
    <PacketField Type="CompactUint" Name="FaceDataLen" />
    <PacketField Type="Bytes" Length="FaceDataLen.Value" Name="Face" />
  </PacketInfo>*/
        [Packet(0x60, false)]
        private void p_ReadPrivateMessage(PWStream pkt)
        {
            pkt.ReadUInt16();
            int sendNickLength = pkt.ReadByte();
            string sendNick = pkt.ReadUString(sendNickLength);
            uint sendId = pkt.ReadUInt32();
            int recvNickLength = pkt.ReadByte();
            string recvNick = pkt.ReadUString(recvNickLength);
            uint recvId = pkt.ReadUInt32();
            int messageLength = pkt.ReadCompactUInt16();
            string message = pkt.ReadUString(messageLength);
            /*
                        <PacketField Type="Byte" Name="Unk" />
                        <PacketField Type="Dword" Name="RecvUID" />*/

            if (ChatMessageReceived != null)
                ChatMessageReceived(new ChatMessage { Type = ChatMessageType.Private, ReceiverNick = recvNick, ReceiverUID = recvId, SenderNick = sendNick, SenderUID = sendId, Message = message });
        }
        //0x60
        private void p_SendPrivateMessage(ChatMessage msg)
        {
            p_SPacket pkt = new p_SPacket(0x60, false);
            pkt.AddUInt16(85);//1877);

            pkt.AddUString(msg.SenderNick);
            pkt.AddUInt32(msg.SenderUID);
            pkt.AddUString(msg.ReceiverNick);
            pkt.AddUInt32(msg.ReceiverUID);

            pkt.AddUString(msg.Message);

            pkt.AddByte(0);
            pkt.AddUInt32(msg.ReceiverUID);
            sendAsync(pkt);
            /*
             *   <PacketInfo Type="0x60" Direction="C2S" Container="False" Name="SendPrivateMessage">
    <PacketField Type="Word" Name="Unk" />
    <PacketField Type="Byte" Name="NickLength" />
    <PacketField Type="UString" Length="NickLength.Value" Name="SendNick" />
    <PacketField Type="Dword" Name="SendUID" />
    <PacketField Type="Byte" Name="NickLength2" />
    <PacketField Type="UString" Length="NickLength2.Value" Name="RecvNick" />
    <PacketField Type="Dword" Name="RecvUID" />
    <PacketField Type="Byte" Name="MessageLength" />
    <PacketField Type="UString" Length="MessageLength.Value" Name="Message" />
    <PacketField Type="Byte" Name="Unk" />
    <PacketField Type="Dword" Name="RecvUID" />
  </PacketInfo>
             * */

        }
        /*
  <PacketInfo Type="0x63" Direction="C2S" Container="True" Name="GetEquipInfo">
    <PacketField Type="Dword" Name="UID" />
  </PacketInfo>
  <PacketInfo Type="0x71" Direction="C2S" Container="True" Name="GenieUpLevel">
    <PacketField Type="Dword" Name="UpLevel" />
    <PacketField Type="Byte" Name="Type" Desc="0 - опыт, 1 - дух" />
  </PacketInfo>*/
        //0x76
        private void p_GetUIDByNick(string nick)
        {
            p_SPacket pkt = new p_SPacket(0x76, false);
            pkt.AddUString(nick);
            pkt.AddUInt32(0);
            pkt.AddByte(0);
            sendAsync(pkt);
            /*<PacketInfo Type="0x76" Direction="C2S" Container="False" Name="GetUIDByName">
<PacketField Type="CompactUint" Name="NameLength" />
<PacketField Type="UString" Length="NameLength.Value" Name="Name" />
<PacketField Type="Dword" Name="Unk" />
<PacketField Type="Byte" Name="unk" />
</PacketInfo>*/
        }
        [Packet(0x77, false)]
        private void p_PlayerUID(PWStream pkt)
        {
            string s = pkt.ReadUString((int)pkt.Length);
            this.Login = s;
            byte[] b = pkt.ReadByte((int)pkt.Length);
            /*
             *   <PacketInfo Type="0x77" Direction="S2C" Container="False" Name="PlayerUID">
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Dword" Name="S04ID" />
    <PacketField Type="CompactUint" Name="NameLength" />
    <PacketField Type="UString" Length="NameLength.Value" Name="Name" />
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="Byte" Name="unk" />
  </PacketInfo>*/
            pkt.ReadUInt32(); //unk
            pkt.ReadUInt32(); //S04ID
            int nickLength = pkt.ReadByte();
            string name = pkt.ReadUString(nickLength);
            uint id = pkt.ReadUInt32();
            pkt.ReadByte(); //unk
        }

        [Packet(0x85, false)]
        private void p_ReadPublicMessage(PWStream pkt)
        {
            byte type = pkt.ReadByte();
            pkt.ReadByte();
            uint id = pkt.ReadUInt32();
            int nickLength = pkt.ReadByte();
            string nick = pkt.ReadUString(nickLength);
            int messageLength = pkt.ReadCompactUInt16();
            string message = pkt.ReadUString(messageLength);
            //<PacketField Type="Byte" Name="Unk" />

            ChatMessageType _type;
            if (type == 1)
                _type = ChatMessageType.World;
            else
                _type = ChatMessageType.Local;
            if (ChatMessageReceived != null)
                ChatMessageReceived(new ChatMessage { Type = _type, ReceiverNick = "", ReceiverUID = 0, SenderNick = nick, SenderUID = id, Message = message });
        }

        [Packet(0x105)]
        private void p_PersonalLockInfo(PWStream pkt)
        {
            byte isEnable = pkt.ReadByte();
            if (isEnable == 0)
            {
                this.IsLockEnable = false;
            }
            else
            {
                this.IsLockEnable = true;
                pkt.ReadUInt32(); //Вместо серверного времени своё
                this.LockDuration = pkt.ReadUInt32();
                this.UnlockTime = DateTime.Now.AddSeconds(LockDuration + 3);
            }

            Thread.Sleep(1000);
            IsInGame = true;
            if (InGame != null)
                InGame();
            p_GetInventory();

            return;
            /*
             *   <PacketInfo Type="0x105" Direction="S2C" Container="True" Name="PersonalLockInfo">
    <PacketField Type="Byte" Name="LockEnable" />
    <PacketField Type="TimeStamp" Name="UnlockTime" />
    <PacketField Type="Dword" Name="Duration" />
  </PacketInfo>
             * */
        }
        /*
  <PacketInfo Type="0x8F" Direction="S2C" Container="False" Name="LastLogin">
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="TimeStamp" Name="LastLoginDate" />
    <PacketField Type="Bytes" Length="4" Name="LastLoginIP" />
    <PacketField Type="Bytes" Length="4" Name="MyIP" />
  </PacketInfo>
  <PacketInfo Type="0xCF" Direction="S2C" Container="False" Name="FriendList">
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="Word" Name="Count" />
    <PacketDataBlock Count="Count.Value" Name="Friend">
      <PacketField Type="Dword" Name="UID" />
      <PacketField Type="Byte" Name="Unk" />
      <PacketField Type="Byte" Name="OnlineFlag" />
      <PacketField Type="Byte" Name="NickLength" />
      <PacketField Type="UString" Length="NickLength.Value" Name="Nick" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x105" Direction="S2C" Container="True" Name="PersonalLockInfo">
    <PacketField Type="Byte" Name="LockEnable" />
    <PacketField Type="TimeStamp" Name="UnlockTime" />
    <PacketField Type="Dword" Name="Duration" />
  </PacketInfo>
  <PacketInfo Type="0x198" Direction="S2C" Container="False" Name="GoldAuction">
    <PacketField Type="Bytes" Length="8" Name="Unk" />
    <PacketField Type="Byte" Name="Count" />
    <PacketDataBlock Count="Count.Value" Name="Lot">
      <PacketField Type="Int32" Name="Price" />
      <PacketField Type="Dword" Name="GoldCount" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x325" Direction="S2C" Container="False" Name="AuctionList">
    <PacketField Type="Dword" Name="IDS04unk" />
    <PacketField Type="Word" Name="ListID" />
    <PacketField Type="Dword" Name="ItemsCountUnk" />
    <PacketField Type="Byte" Name="ItemsCountDouble" />
    <PacketDataBlock Count="ItemsCountDouble.Value" Name="Items">
      <PacketField Type="Dword" Name="ItemID" />
      <PacketField Type="Dword" Name="StartPrice" />
      <PacketField Type="Dword" Name="EndPrice" />
      <PacketField Type="TimeStamp" Name="Time" />
      <PacketField Type="Word" Name="Unk" />
      <PacketField Type="Word" Name="ItemID" />
      <PacketField Type="Word" Name="Count" />
      <PacketField Type="Dword" Name="Unk" />
      <PacketField Type="Bytes" Length="4" Name="Unk" Desc="Unk" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x335" Direction="S2C" Container="False" Name="AucItemInfo">
    <PacketField Type="Dword" Name="S04ID" />
    <PacketField Type="Byte" Name="LotCount" />
    <PacketDataBlock Count="LotCount.Value" Name="Lots">
      <PacketField Type="Dword" Name="Lot" />
    </PacketDataBlock>
    <PacketField Type="Byte" Name="Count" />
  </PacketInfo>
  <PacketInfo Type="0x353" Direction="S2C" Container="False" Name="Territoryes">
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Byte" Name="Count" />
    <PacketDataBlock Count="Count.Value" Name="Territory">
      <PacketField Type="Byte" Name="TerritoryID" />
      <PacketField Type="Byte" Name="TerritoryLevel" />
      <PacketField Type="Byte" Name="unk" />
      <PacketField Type="Dword" Name="GuildID" />
      <PacketField Type="Dword" Name="AttackGuild" />
      <PacketField Type="TimeStamp" Name="Date" />
      <PacketField Type="Bytes" Length="8" Name="Unk" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x1197" Direction="S2C" Container="False" Name="GuildInfo">
    <PacketField Type="Bytes" Length="12" Name="Unk12" />
    <PacketField Type="Byte" Name="GuildTextLen" />
    <PacketField Type="UString" Length="GuildTextLen.Value" Name="GuildText" />
    <PacketField Type="Byte" Name="unk" />
    <PacketField Type="Byte" Name="PlayerCount" />
    <PacketDataBlock Count="PlayerCount.Value" Name="Player">
      <PacketField Type="Dword" Name="UID" />
      <PacketField Type="Byte" Name="Level" />
      <PacketField Type="Dword" Name="Unk" />
      <PacketField Type="Word" Name="NameLen" />
      <PacketField Type="UString" Length="NameLen.Value" Name="Name" />
      <PacketField Type="Byte" Name="TitleLen" />
      <PacketField Type="UString" Length="TitleLen.Value" Name="Title" />
      <PacketField Type="Dword" Name="&quot;Score&quot;" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x1199" Direction="S2C" Container="False" Name="JoinTheGuild">
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Dword" Name="Unk" />
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Dword" Name="InvitedUID" />
  </PacketInfo>
  <PacketInfo Type="0x12C3" Direction="S2C" Container="False" Name="GuildMessage">
    <PacketField Type="Word" Name="unk" />
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="CompactUint" Name="MessLen" />
    <PacketField Type="UString" Length="MessLen.Value" Name="Mess" />
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Byte" Name="unk" />
  </PacketInfo>
  <PacketInfo Type="0x12C3" Direction="C2S" Container="False" Name="GuildMessage">
    <PacketField Type="Word" Name="unk" />
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="CompactUint" Name="MessLen" />
    <PacketField Type="UString" Length="MessLen.Value" Name="Mess" />
    <PacketField Type="Dword" Name="unk" />
    <PacketField Type="Byte" Name="unk" />
  </PacketInfo>
  <PacketInfo Type="0x12CE" Direction="C2S" Container="False" Name="GetGuildInfo">
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Bytes" Length="4" Name="Unknown" />
    <PacketField Type="CompactUint" Name="Count" />
    <PacketDataBlock Count="Count.Value" Name="GuildsID">
      <PacketField Type="Dword" Name="GuildID" />
    </PacketDataBlock>
  </PacketInfo>
  <PacketInfo Type="0x12CF" Direction="S2C" Container="False" Name="GuildInfo">
    <PacketField Type="Dword" Name="UID" />
    <PacketField Type="Dword" Name="S04ID" />
    <PacketField Type="Dword" Name="GuildID" />
    <PacketField Type="Byte" Name="Length" />
    <PacketField Type="UString" Length="Length.Value" Name="Name" />
    <PacketField Type="Byte" Name="GuildLevel" />
    <PacketField Type="Word" Name="PlyersCount" />
  </PacketInfo>
  <PacketInfo Type="0x12D1" Direction="S2C" Container="False" Name="Unk">
    <PacketField Type="Dword" Name="MyUID" />
    <PacketField Type="Bytes" Length="8" Name="Unk" />
    <PacketField Type="Byte" Name="NickLength" />
    <PacketField Type="UString" Length="NickLength.Value" Name="Nick" />
  </PacketInfo>
         * */
        /*private byte[] p_CreateGuardPacket()
        {
            return new byte[] { 0x1389 };
        }
        private void p_GuardPacket(byte[] buff)
        {
            //    <PacketField Type="Dword" Name="CharUId" />
        }*/

        //DUB CODE
        [Packet(0xA6)]
        private void p_PlayerToMarket(PWStream pkt)
        {
            uint id = pkt.ReadUInt32();
            var plr = GameObjects[id];
            plr.Type = GameObjectType.Market;
            GameObjects[id] = plr;
            /*
             *   <PacketInfo Type="0x23" Direction="C2S" Container="True" Name="OpenMarket">
    <PacketField Type="Dword" Name="UID" />
  </PacketInfo>
             * */
        }

        [Packet(0x1E)]
        private void p_MoneyIncrement(PWStream pkt)
        {
            Money += pkt.ReadUInt32();
        }
        
    }
}
