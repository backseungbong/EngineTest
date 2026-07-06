namespace Legacy.ECM_Core.Chart
{
    public class DetectionChart
    {
        public DCC.DSID DSID { get; protected set; }
        public DCC.DSPM DSPM { get; protected set; }

        public List<DCC.Vector>? VI { get; protected set; } // Isolate Node
        public List<DCC.Vector>? VC { get; protected set; } // Connected Node
        public List<DCC.Vector>? VE { get; protected set; } // Edge

        public List<DCC.Feature>? Feature { get; protected set; }
        public Dictionary<uint, DCC.FeatureLinker>? FeatureLinker_Collection { get; internal set; }

        public string Name { get; internal set; } = "";
        public (double North, double South, double East, double West) Boundary { get; internal set; } // 이 타입은 property로 만들면 안 쪽을 수정 못함

        public double COMF { get; protected set; } = 0.0;
        public double SOMF { get; protected set; } = 0.0;

        public (int EDTN, int UPDN) Base { get; protected set; } = (-1, -1);
        public int Update { get; internal set; } = -1;
        public int UpdateReference { get; internal set; } = 0;
        public string Update_Date { get; internal set; } = "";

        public int Checksum { get; protected set; } = -1;
        public bool Linked { get; internal set; } = false;

        public List<DCC.UpdateRecord>? Update_Record { get; protected set; }

        public JHLib.ChartManager.ENC.SerialEnc? serialEnc = null;



        public DetectionChart(DetectionCell base_cell)
        {
            if (!base_cell.Absorbed)
            {
                this.DSID = base_cell.DSID;
                this.DSPM = base_cell.DSPM;

                this.VI = base_cell.VI;
                this.VC = base_cell.VC;
                this.VE = base_cell.VE;
                this.Feature = base_cell.Feature;

                base_cell.VI = null;
                base_cell.VC = null;
                base_cell.VE = null;
                base_cell.Feature = null;

                this.Boundary = base_cell.Boundary;

                this.COMF = base_cell.DSPM.COMF;
                this.SOMF = base_cell.DSPM.SOMF;

                this.Base = (EDTN: base_cell.Edition_Number, UPDN: base_cell.Update_Number);
                this.Update = base_cell.Update_Number;
                this.Update_Date = base_cell.DSID.UPDT;

                this.Checksum = this.Base.UPDN;

                base_cell.Absorbed = true;
            }
        }



        public void Absorb(DetectionCell update_cell)
        {
            if (!update_cell.Absorbed && (update_cell.Edition_Number == this.Base.EDTN) && (update_cell.Update_Number > this.Update))
            {
                bool updateReview = update_cell.Update_Number > this.UpdateReference;

                if (update_cell.VI != null) { Absorb_Vector(this.VI ??= new List<DCC.Vector>(), update_cell.VI, updateReview); }
                if (update_cell.VC != null) { Absorb_Vector(this.VC ??= new List<DCC.Vector>(), update_cell.VC, updateReview); }
                if (update_cell.VE != null) { Absorb_Vector(this.VE ??= new List<DCC.Vector>(), update_cell.VE, updateReview); }
                if (update_cell.Feature != null) { Absorb_Feature(this.Feature ??= new List<DCC.Feature>(), update_cell.Feature, updateReview); }

                update_cell.VI = null;
                update_cell.VC = null;
                update_cell.VE = null;
                update_cell.Feature = null;

                this.Update = update_cell.Update_Number;

                //this.Update_Date = update_cell.DSID.UPDT;
                this.Update_Date = update_cell.DSID.ISDT;

                this.Checksum += update_cell.Update_Number;

                update_cell.Absorbed = true;
            }
        }

        public bool Get_Vector(byte rcnm, uint rcid, out DCC.Vector vector)
        {
            bool Result = false;
            vector = null;

            switch (rcnm)
            {
                case 110:
                    if (this.VI != null)
                    {
                        IEnumerable<DCC.Vector> Vector_Enumeration = this.VI.Where(Vector => Vector.VRID.RCID == rcid);
                        
                        if (Vector_Enumeration.Count() > 0)
                        {
                            vector = Vector_Enumeration.First();
                            Result = true;
                        }
                    }
                    break;
                case 120:
                    if (this.VC != null)
                    {
                        IEnumerable<DCC.Vector> Vector_Enumeration = this.VC.Where(Vector => Vector.VRID.RCID == rcid);

                        if (Vector_Enumeration.Count() > 0)
                        {
                            vector = Vector_Enumeration.First();
                            Result = true;
                        }
                    }
                    break;
                case 130:
                    if (this.VE != null)
                    {
                        IEnumerable<DCC.Vector> Vector_Enumeration = this.VE.Where(Vector => Vector.VRID.RCID == rcid);

                        if (Vector_Enumeration.Count() > 0)
                        {
                            vector = Vector_Enumeration.First();
                            Result = true;
                        }
                    }
                    break;
            }

            return Result;
        }


        private void Absorb_Vector(List<DCC.Vector> origin, List<DCC.Vector> update, bool updateReview)
        {
            foreach (DCC.Vector Update_Vector in update)
            {
                IEnumerable<DCC.Vector> OriginVector_Enumeration = origin.Where(Vector => Vector.VRID.RCID == Update_Vector.VRID.RCID);

                switch (Update_Vector.VRID.RUIN)
                {
                    case 1:
                        if (OriginVector_Enumeration.Count() > 0)
                        {
                            // can't insert
                        }
                        else
                        {
                            origin.Add(Update_Vector);

                            if (updateReview)
                            {
                                DCC.UpdateRecord Update_Record = new DCC.UpdateRecord();
                                Update_Record.VRID = Update_Vector.VRID;

                                if (Update_Vector.SG2D?.Count > 0)
                                {
                                    Update_Record.SG2D ??= new List<DCC.SG2D>();

                                    foreach (DCC.SG2D Update_SG2D in Update_Vector.SG2D)
                                    {
                                        Update_Record.SG2D.Add(Update_SG2D);
                                    }
                                }

                                if (Update_Vector.SG3D?.Count > 0)
                                {
                                    Update_Record.SG2D ??= new List<DCC.SG2D>();

                                    foreach (DCC.SG3D Update_SG3D in Update_Vector.SG3D)
                                    {
                                        Update_Record.SG2D.Add(new DCC.SG2D() {
                                            XCOO = Update_SG3D.XCOO,
                                            YCOO = Update_SG3D.YCOO,
                                        });
                                    }
                                }

                                this.Update_Record ??= new List<DCC.UpdateRecord>();
                                this.Update_Record.Add(Update_Record);
                            }
                        }
                        break;
                    case 2:
                        if (OriginVector_Enumeration.Count() > 0)
                        {
                            DCC.Vector Origin_Vector = OriginVector_Enumeration.First();
                            Origin_Vector.VRID = Update_Vector.VRID;

                            if (updateReview)
                            {
                                DCC.UpdateRecord Update_Record = new DCC.UpdateRecord();
                                Update_Record.VRID = Update_Vector.VRID;

                                if (Origin_Vector.SG2D?.Count > 0)
                                {
                                    Update_Record.SG2D ??= new List<DCC.SG2D>();

                                    foreach (DCC.SG2D Origin_SG2D in Origin_Vector.SG2D)
                                    {
                                        Update_Record.SG2D.Add(Origin_SG2D);
                                    }
                                }

                                if (Origin_Vector.SG3D?.Count > 0)
                                {
                                    Update_Record.SG2D ??= new List<DCC.SG2D>();

                                    foreach (DCC.SG3D Origin_SG2D in Origin_Vector.SG3D)
                                    {
                                        Update_Record.SG2D.Add(new DCC.SG2D() {
                                            XCOO = Origin_SG2D.XCOO,
                                            YCOO = Origin_SG2D.YCOO,
                                        });
                                    }
                                }

                                this.Update_Record ??= new List<DCC.UpdateRecord>();
                                this.Update_Record.Add(Update_Record);
                            }
                        }
                        else
                        {
                            // can't delete
                        }
                        break;
                    case 3:
                        if (OriginVector_Enumeration.Count() > 0)
                        {
                            DCC.Vector Origin_Vector = OriginVector_Enumeration.First();

                            List<DCC.UpdateRecord>? UpdateRecord_Collection = Origin_Vector.ModifyUpdate_SGCC(Update_Vector);
                            Origin_Vector.ModifyUpdate_VRPC(Update_Vector);
                            Origin_Vector.ModifyUpdate_ATTV(Update_Vector);

                            Origin_Vector.VRID = Update_Vector.VRID;


                            if (updateReview && (UpdateRecord_Collection != null))
                            {
                                if (this.Update_Record == null)
                                {
                                    this.Update_Record = UpdateRecord_Collection;
                                }
                                else
                                {
                                    this.Update_Record.AddRange(UpdateRecord_Collection);
                                }
                            }
                        }
                        else
                        {
                            // can't modify
                        }
                        break;
                }
            }
        }

        private void Absorb_Feature(List<DCC.Feature> origin, List<DCC.Feature> update, bool updateReview)
        {
            foreach (DCC.Feature Update_Feature in update)
            {
                IEnumerable<DCC.Feature> OriginFeature_Enumeration = origin.Where(Feature => Feature.FRID.RCID == Update_Feature.FRID.RCID);

                switch (Update_Feature.FRID.RUIN)
                {
                    case 1:
                        if (OriginFeature_Enumeration.Count() > 0)
                        {
                            // can't insert
                        }
                        else
                        {
                            if (updateReview)
                            {
                                Update_Feature.Update_Type = 11; // Update Review에 해당하는 Insert Feature. 항상 그려짐.
                            }
                            else
                            {
                                Update_Feature.Update_Type = 1; // Update Review에 해당하지 않는 Insert Feature. 항상 그려짐.
                            }

                            origin.Add(Update_Feature);
                        }
                        break;
                    case 2:
                        if (OriginFeature_Enumeration.Count() > 0)
                        {
                            DCC.Feature Origin_Feature = OriginFeature_Enumeration.First();
                            Origin_Feature.FRID = Update_Feature.FRID;

                            if (updateReview)
                            {
                                Origin_Feature.Update_Type = 12; // Update Review에 해당하는 Delete Feature. Update Review일 때만 그려짐.
                            }
                            else
                            {
                                Origin_Feature.Update_Type = 2; // Update Review에 해당하지 않는 Delete Feature. 항상 그려지지 않음.
                            }
                        }
                        else
                        {
                            // can't delete
                        }
                        break;
                    case 3:
                        if (OriginFeature_Enumeration.Count() > 0)
                        {
                            DCC.Feature Origin_Feature = OriginFeature_Enumeration.First();

                            Origin_Feature.ModifyUpdate_FSPC(Update_Feature);
                            Origin_Feature.ModifyUpdate_FFPT(Update_Feature);
                            Origin_Feature.ModifyUpdate_ATTF(Update_Feature);
                            Origin_Feature.ModifyUpdate_NATF(Update_Feature);
                            
                            Origin_Feature.FRID = Update_Feature.FRID;

                            if (updateReview)
                            {
                                Origin_Feature.Update_Type = 13; // Update Review에 해당하는 Modify Feature. 항상 그려짐.
                            }
                            else
                            {
                                Origin_Feature.Update_Type = 3; // Update Review에 해당하지 않는 Modify Feature. 항상 그려짐.
                            }
                        }
                        else
                        {
                            // can't modify
                        }
                        break;
                }
            }
        }
    }
}