namespace Generator
{
    public abstract class TemplateBase
    {
        public string Value { get; set; }
        public abstract void Execute();

        public string Result { get; private set; }
        
        public virtual void Write(object value)
        {
            Result += value.ToString();
        }

        public virtual void WriteLiteral(object value)
        {
            Result += value.ToString();
        }
    }
}