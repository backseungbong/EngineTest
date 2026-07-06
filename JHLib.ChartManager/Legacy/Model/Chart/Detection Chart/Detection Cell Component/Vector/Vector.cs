namespace Legacy.ECM_Core.DCC
{
    public class Vector
    {
        public VRID VRID;

        public List<ATTV>? ATTV;
        public List<VRPT>? VRPT;
        public List<VRPC>? VRPC;

        public List<SGCC>? SGCC;
        public List<SG2D>? SG2D;
        public List<SG3D>? SG3D;

        public List<Feature>? Linked_Feature;

        public bool Bound = true;



        public List<UpdateRecord>? ModifyUpdate_SGCC(Vector update)
        {
            List<UpdateRecord> UpdateRecord_Collection = new List<UpdateRecord>();

            if (update.SGCC == null)
            {
                if ((update.SG2D?.Count > 0) || (update.SG3D?.Count > 0))
                {
                    if (update.SG2D?.Count > 0)
                    {
                        this.SG2D = new List<SG2D>(update.SG2D);

                        UpdateRecord Update_Record = new UpdateRecord();
                        Update_Record.VRID = update.VRID;
                        Update_Record.SG2D = new List<SG2D>(update.SG2D);

                        UpdateRecord_Collection.Add(Update_Record);
                    }

                    if (update.SG3D?.Count > 0)
                    {
                        this.SG3D = new List<SG3D>(update.SG3D);

                        UpdateRecord Update_Record = new UpdateRecord();
                        Update_Record.VRID = update.VRID;
                        Update_Record.SG2D = new List<SG2D>();

                        update.SG3D.ForEach(SG3D => {
                            Update_Record.SG2D.Add(new SG2D() {
                                XCOO = SG3D.XCOO,
                                YCOO = SG3D.YCOO,
                            });
                        });

                        UpdateRecord_Collection.Add(Update_Record);
                    }

                    return UpdateRecord_Collection;
                }
                else
                {
                    return null;
                }
            }


            foreach (SGCC Update_SGCC in update.SGCC)
            {
                switch (Update_SGCC.CCUI)
                {
                    case 1:
                        {
                            UpdateRecord? Update_Record_2D = null;
                            UpdateRecord? Update_Record_3D = null;

                            if (update.SG2D?.Count > 0)
                            {
                                Update_Record_2D = new UpdateRecord();
                                Update_Record_2D.VRID = update.VRID;
                                Update_Record_2D.VRID.RUIN = 1;
                            }

                            if (update.SG3D?.Count > 0)
                            {
                                Update_Record_3D = new UpdateRecord();
                                Update_Record_3D.VRID = update.VRID;
                                Update_Record_3D.VRID.RUIN = 1;
                            }

                            for (int i = 0; i < Update_SGCC.CCNC; i++)
                            {
                                int j = Update_SGCC.CCIX - 1 + i;

                                if (j >= 0)
                                {
                                    if ((i < update.SG2D?.Count) && (j <= (this.SG2D?.Count ?? 0)))
                                    {
                                        this.SG2D ??= new List<SG2D>();
                                        this.SG2D.Insert(j, update.SG2D[i]);

                                        Update_Record_2D!.SG2D ??= new List<SG2D>();
                                        Update_Record_2D.SG2D.Add(update.SG2D[i]);
                                    }

                                    if ((i < update.SG3D?.Count) && (j <= (this.SG3D?.Count ?? 0)))
                                    {
                                        this.SG3D ??= new List<SG3D>();
                                        this.SG3D.Insert(j, update.SG3D[i]);

                                        Update_Record_3D!.SG2D ??= new List<SG2D>();
                                        Update_Record_3D.SG2D.Add(new SG2D() {
                                            XCOO = update.SG3D[i].XCOO,
                                            YCOO = update.SG3D[i].YCOO,
                                        });
                                    }
                                }
                            }

                            if (Update_Record_2D != null) { UpdateRecord_Collection.Add(Update_Record_2D); }
                            if (Update_Record_3D != null) { UpdateRecord_Collection.Add(Update_Record_3D); }
                        }
                        break;
                    case 2:
                        {
                            UpdateRecord Update_Record = new UpdateRecord();
                            Update_Record.VRID = update.VRID;
                            Update_Record.VRID.RUIN = 2;

                            for (int i = Update_SGCC.CCNC - 1; i >= 0; i--)
                            {
                                int j = Update_SGCC.CCIX - 1 + i;

                                if (j >= 0)
                                {
                                    if ((0 < this.SG2D?.Count) && (j < this.SG2D?.Count))
                                    {
                                        Update_Record.SG2D ??= new List<SG2D>();
                                        Update_Record.SG2D.Add(this.SG2D[j]);

                                        this.SG2D.RemoveAt(j);
                                    }

                                    if ((0 < this.SG3D?.Count) && (j < this.SG3D?.Count))
                                    {
                                        Update_Record.SG2D ??= new List<SG2D>();
                                        Update_Record.SG2D.Add(new SG2D() {
                                            XCOO = this.SG3D[j].XCOO,
                                            YCOO = this.SG3D[j].YCOO,
                                        });

                                        this.SG3D.RemoveAt(j);
                                    }
                                }
                            }

                            UpdateRecord_Collection.Add(Update_Record);


                            if (update.SG2D?.Count > 0)
                            {
                                Update_Record = new UpdateRecord();
                                Update_Record.VRID = update.VRID;
                                Update_Record.VRID.RUIN = 1;
                                Update_Record.SG2D = new List<SG2D>();

                                if (this.SG2D != null)
                                {
                                    this.SG2D.Clear();
                                }
                                else
                                {
                                    this.SG2D = new List<SG2D>();
                                }

                                foreach (SG2D Update_SG2D in update.SG2D)
                                {
                                    this.SG2D.Add(Update_SG2D);

                                    Update_Record.SG2D.Add(Update_SG2D);
                                }

                                UpdateRecord_Collection.Add(Update_Record);
                            }

                            if (update.SG3D?.Count > 0)
                            {
                                Update_Record = new UpdateRecord();
                                Update_Record.VRID = update.VRID;
                                Update_Record.VRID.RUIN = 1;
                                Update_Record.SG2D = new List<SG2D>();

                                if (this.SG3D != null)
                                {
                                    this.SG3D.Clear();
                                }
                                else
                                {
                                    this.SG3D = new List<SG3D>();
                                }

                                foreach (SG3D Update_SG3D in update.SG3D)
                                {
                                    this.SG3D.Add(Update_SG3D);

                                    Update_Record.SG2D.Add(new SG2D() {
                                        XCOO = Update_SG3D.XCOO,
                                        YCOO = Update_SG3D.YCOO,
                                    });
                                }

                                UpdateRecord_Collection.Add(Update_Record);
                            }
                        }
                        break;
                    case 3:
                        {
                            for (int i = 0; i < Update_SGCC.CCNC; i++)
                            {
                                int j = Update_SGCC.CCIX - 1 + i;

                                if (j >= 0)
                                {
                                    if ((i < update.SG2D?.Count) && (j < this.SG2D?.Count))
                                    {
                                        this.SG2D[j] = update.SG2D[i];
                                    }

                                    if ((i < update.SG3D?.Count) && (j < this.SG3D?.Count))
                                    {
                                        this.SG3D[j] = update.SG3D[i];
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            return UpdateRecord_Collection;
        }

        public void ModifyUpdate_VRPC(Vector update)
        {
            if (update.VRPC == null) { return; }


            foreach (VRPC Update_VRPC in update.VRPC)
            {
                if ((Update_VRPC.VPUI != 1) && !(this.VRPT?.Count > 0)) { break; }
                if ((Update_VRPC.VPUI != 2) && !(update.VRPT?.Count > 0)) { break; }

                switch (Update_VRPC.VPUI)
                {
                    case 1:
                        for (int i = 0; i < Update_VRPC.NVPT; i++)
                        {
                            int j = Update_VRPC.VPIX - 1 + i;

                            if (j >= 0)
                            {
                                if ((i < update.VRPT?.Count) && (j <= (this.VRPT?.Count ?? 0)))
                                {
                                    this.VRPT ??= new List<VRPT>();
                                    this.VRPT.Insert(j, update.VRPT[i]);
                                }
                            }
                        }
                        break;
                    case 2:
                        for (int i = Update_VRPC.NVPT - 1; i >= 0; i--)
                        {
                            int j = Update_VRPC.VPIX - 1 + i;

                            if (j >= 0)
                            {
                                if (j < this.VRPT?.Count)
                                {
                                    this.VRPT.RemoveAt(j);
                                }
                            }
                        }
                        break;
                    case 3:
                        for (int i = 0; i < Update_VRPC.NVPT; i++)
                        {
                            int j = Update_VRPC.VPIX - 1 + i;

                            if (j >= 0)
                            {
                                if ((i < update.VRPT?.Count) && (j < this.VRPT?.Count))
                                {
                                    this.VRPT[j] = update.VRPT[i];
                                }
                            }
                        }
                        break;
                }
            }
        }

        public void ModifyUpdate_ATTV(Vector update)
        {
            if (update.ATTV == null) { return; }


            if (this.ATTV == null)
            {
                this.ATTV = new List<ATTV>(update.ATTV);
            }
            else
            {
                for (int i = 0; i < update.ATTV.Count; i++)
                {
                    if (i < this.ATTV.Count)
                    {
                        this.ATTV[i] = update.ATTV[i];
                    }
                    else
                    {
                        this.ATTV.Add(update.ATTV[i]);
                    }
                }
            }
        }
    }
}