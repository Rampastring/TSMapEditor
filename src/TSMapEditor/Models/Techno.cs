using Microsoft.Xna.Framework;

namespace TSMapEditor.Models
{
    public abstract class Techno<T> : TechnoBase where T : TechnoType
    {
        public Techno(T objectType)
        {
            ObjectType = objectType;
        }

        public override double GetWeaponRange() => ObjectType.GetWeaponRange();

        public override double GetGuardRange()
        {
            if (ObjectType.GuardRange == 0.0)
                return GetWeaponRange();

            return ObjectType.GuardRange > 0.0 ? ObjectType.GuardRange : GetWeaponRange();
        }

        public T ObjectType { get; }
    }

    public abstract class TechnoBase : GameObject
    {
        public TechnoBase()
        {
            HP = Constants.ObjectHealthMax;
        }

        public virtual House Owner { get; set; }
        public int HP { get; set; }
        public virtual byte Facing { get; set; }
        public Tag AttachedTag { get; set; }

        public abstract double GetWeaponRange();
        public abstract double GetGuardRange();

        public override Color GetRemapColor() => Remapable() ? Owner.XNAColor : Color.White;
    }
}
