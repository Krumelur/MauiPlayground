using System.ComponentModel;
using AntLogic;
using PropertyChanged;

namespace MauiPlaygroundApp;

[AddINotifyPropertyChangedInterface]
public partial class AntPage : ContentPage, IQueryAttributable, INotifyPropertyChanged
{
	Ant _ant;
	AntGrid _grid;
	int _moveSpeed;

	public AntPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	readonly Image _antImage = new()
	{
		Source = "ant.png",
		Aspect = Aspect.AspectFit
	};

	readonly Grid _antImageContainer = new();

	protected async override void OnAppearing()
	{
		base.OnAppearing();

		// Subscribe to an event so we can resize the ant image if the grid changed its size.
		// By the way: Unfortunately, there's a (known) issue with the GridLayout not resizing on Mac Catalyst
		// when the window size changes. Workaround would be to customize grid and do our own
		// measurements.
		AntGridLayout.SizeChanged += AntGridLayout_SizeChanged;

		// Let the ant run while there are steps left.
		while (_ant.StepsLeft > 0)
		{
			UpdateView();
			await Task.Delay(_moveSpeed);
			_ant.Move();
		}
	}

	/// <summary>
	/// Helper to update the visuals.
	/// This places the ant at the correct position and updates the grid colors.
	/// </summary>
	void UpdateView()
	{
		// Put the ant in the right position.
		AntGridLayout.SetColumn(_antImageContainer, _ant.Position.x);
		AntGridLayout.SetRow(_antImageContainer, _ant.Position.y);
		_antImage.Rotation = _ant.Direction switch
		{
			Ant.AntDirection.North => 0,
			Ant.AntDirection.South => 180,
			Ant.AntDirection.West => 270,
			Ant.AntDirection.East => 90,
			_ => 0
		};

		// Update the box views in the grid layout to reflect the state of
		// the grid model.
		for (int row = 0; row < _grid.Height; row++)
		{
			for (int col = 0; col < _grid.Width; col++)
			{
				var boxView = AntGridLayout.Children.FirstOrDefault(
					view =>
						AntGridLayout.GetRow(view) == row
						&& AntGridLayout.GetColumn(view) == col
					) as BoxView;

				var posColor = _grid[col, row];
				boxView.Color =
					posColor == AntGrid.CellColor.Black
					? Colors.DarkGray
					: Colors.White;
			}
		}
	}

	protected override void OnDisappearing()
	{
		// By setting the remaining steps to zero we stop the ant's movement,
		// therefore allowin the async OnAppearing() method to return.
		_ant.StepsLeft = 0;

		// Don't forget to unsubscribe, otherwise we'd introduce a memory leak.
		AntGridLayout.SizeChanged -= AntGridLayout_SizeChanged;

		base.OnDisappearing();
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		_ant = query["ant"] as Ant;
		_grid = query["grid"] as AntGrid;
		_moveSpeed = Convert.ToInt32(query["moveSpeed"]);

		// Create a grid layout based on the ant's grid model.
		var colDefs = Enumerable
			.Repeat(new ColumnDefinition
			{
				Width = GridLength.Star
			}, _grid.Width)
			.ToArray();

		var rowDefs = Enumerable
			.Repeat(new RowDefinition
			{
				Height = GridLength.Star
			}, _grid.Height)
			.ToArray();

		AntGridLayout.ColumnDefinitions = new ColumnDefinitionCollection(colDefs);
		AntGridLayout.RowDefinitions = new RowDefinitionCollection(rowDefs);

		for (int row = 0; row < _grid.Height; row++)
		{
			for (int col = 0; col < _grid.Width; col++)
			{
				var box = new BoxView
				{
					Color = Colors.Gray
				};
				AntGridLayout.Add(box, col, row);
			}
		}

		// When putting an image into a grid layout cell, the image's width and height
		// requests are ignored by MAUI. What helps is wrapping the image in a container
		// (a simple 1x1 grid layout) which can then be resized just fine.
		_antImageContainer.Add(_antImage, 0, 0);
		AntGridLayout.Add(_antImageContainer, 0, 0);

		// Bind the dimensions of the ant image to the sizes we caculate in reaction to the grid
		// layout size changing. This is probably only useful on MacCatalyst where the window can be
		// resized.
		_antImageContainer.SetBinding(Image.WidthRequestProperty, new Binding("AntWidth"));
		_antImageContainer.SetBinding(Image.HeightRequestProperty, new Binding("AntHeight"));
	}

	private void AntGridLayout_SizeChanged(object sender, EventArgs e)
	{
		OnPropertyChanged(nameof(AntWidth));
		OnPropertyChanged(nameof(AntHeight));
	}

	/// <summary>
	/// Returns the ant's width depending on the current grid layout size.
	/// </summary>
	public double AntWidth => Width / AntGridLayout.ColumnDefinitions.Count * 0.75;

	/// <summary>
	/// Returns the ant's height depending on the current grid layout size.
	/// </summary>
	public double AntHeight => Height / AntGridLayout.RowDefinitions.Count * 0.75;

	/// <summary>
	/// The current ASCII representation.
	/// </summary>
	public string AntString => _grid != null ? _grid.GetGridAsString(_ant) : string.Empty;
}
