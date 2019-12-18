using Newtonsoft.Json;
using System.Text;

public interface ITransferModel
{
    byte[] ToBytes();
    string ToJson();
}

public abstract class TransferModel : ITransferModel
{
    /// <summary>
    /// Return this object as Json.
    /// </summary>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    /// <summary>
    /// Return a Byte array of this object.
    /// </summary>
    public byte[] ToBytes()
    {
        return Encoding.UTF8.GetBytes(ToJson());
    }


    public static class TransferModelFactory<TModel> where TModel: ITransferModel
    {
        public static TModel FromJson(string jsonString)
        {
            return JsonConvert.DeserializeObject<TModel>(jsonString);
        }

        public static TModel FromBytes(byte[] bytes)
        {
            return FromJson(Encoding.UTF8.GetString(bytes));
        }
    }
}
