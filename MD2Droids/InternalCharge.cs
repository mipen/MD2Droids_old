
namespace MD2
{
    public interface InternalCharge
    {
        float TotalCharge
        {
            get;
            set;
        }
        bool AddPowerDirect(float amount);
        bool RemovePowerDirect(float amount);
        bool Charge(float rate);
        bool Deplete(float rate);
        bool DesiresCharge();
    }
}
