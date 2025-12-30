using dlapp.ViewModels;

namespace dlapp.Tests.Unit.ViewModels;

public class VideoItemTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var item = new VideoItem();

        item.Title.Should().BeEmpty();
        item.Status.Should().Be("Pending");
        item.Index.Should().BeEmpty();
        item.IsCompleted.Should().BeFalse();
        item.IsDownloading.Should().BeFalse();
    }

    [Theory]
    [InlineData("Pending", false, false)]
    [InlineData("Downloading...", false, true)]
    [InlineData("Completed", true, false)]
    [InlineData("Error", false, false)]
    public void Status_UpdatesComputedProperties(string status, bool expectedIsCompleted, bool expectedIsDownloading)
    {
        var item = new VideoItem
        {
            Status = status
        };

        item.IsCompleted.Should().Be(expectedIsCompleted);
        item.IsDownloading.Should().Be(expectedIsDownloading);
    }

    [Fact]
    public void Title_TriggersPropertyChanged()
    {
        var item = new VideoItem();
        var changedProperties = new List<string>();
        item.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        item.Title = "Test Video";

        changedProperties.Should().Contain(nameof(VideoItem.Title));
    }

    [Fact]
    public void Index_TriggersPropertyChanged()
    {
        var item = new VideoItem();
        var changedProperties = new List<string>();
        item.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        item.Index = "1";

        changedProperties.Should().Contain(nameof(VideoItem.Index));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("01")]
    [InlineData("001")]
    public void Index_VariousFormats_StoredCorrectly(string index)
    {
        var item = new VideoItem { Index = index };

        item.Index.Should().Be(index);
    }

    [Fact]
    public void IsCompleted_False_WhenStatusIsDownloading()
    {
        var item = new VideoItem { Status = "Downloading..." };

        item.IsCompleted.Should().BeFalse();
        item.IsDownloading.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_True_WhenStatusIsCompleted()
    {
        var item = new VideoItem { Status = "Completed" };

        item.IsCompleted.Should().BeTrue();
        item.IsDownloading.Should().BeFalse();
    }

    [Fact]
    public void IsDownloading_False_WhenStatusIsPending()
    {
        var item = new VideoItem { Status = "Pending" };

        item.IsCompleted.Should().BeFalse();
        item.IsDownloading.Should().BeFalse();
    }
}
