namespace ReqnrollCalculator;

public class Calculator
{
    private readonly Stack<int> _numbers = new();

    public void Enter(int number)
    {
        _numbers.Push(number);
    }

    public void Reset()
    {
        _numbers.Clear();
    }

    public int GetResult()
    {
        return _numbers.Peek();
    }

    public string GetResultMessage()
    {
        return $"The result is {GetResult()}.";
    }

    public void Add()
    {
        _numbers.Push(_numbers.Pop() + _numbers.Pop());
    }

    public void Multiply()
    {
        _numbers.Push(_numbers.Pop() * _numbers.Pop());
    }
}