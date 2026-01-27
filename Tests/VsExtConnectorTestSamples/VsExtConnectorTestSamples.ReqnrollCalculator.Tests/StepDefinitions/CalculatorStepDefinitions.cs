namespace ReqnrollCalculator.Specs.StepDefinitions;

[Binding]
public sealed class CalculatorStepDefinitions
{
    private readonly Calculator _calculator = new();

    [Given("the first number is {int}")]
    public void GivenTheFirstNumberIs(int number)
    {
        _calculator.Reset();
        _calculator.Enter(number);
    }

    [Given("the second number is {int}")]
    public void GivenTheSecondNumberIs(int number)
    {
        _calculator.Enter(number);
    }

    [Given("the entered numbers are")]
    public void GivenTheEnteredNumbersAre(DataTable dataTable)
    {
        _calculator.Reset();
        foreach (var row in dataTable.Rows)
        {
            _calculator.Enter(int.Parse(row[0]));
        }
    }

    [When("the two numbers are added")]
    public void WhenTheTwoNumbersAreAdded()
    {
        _calculator.Add();
    }

    [When("the two numbers are multiplied")]
    public void WhenTheTwoNumbersAreMultiplied()
    {
        _calculator.Multiply();
    }

    [Then("the result should be {int}")]
    public void ThenTheResultShouldBe(int result)
    {
        _calculator.GetResult().Should().Be(result);
    }

    [Then("the text message should be")]
    public void ThenTheTextMessageShouldBe(string expectedMessage)
    {
        _calculator.GetResultMessage().Should().Be(expectedMessage);
    }

}
