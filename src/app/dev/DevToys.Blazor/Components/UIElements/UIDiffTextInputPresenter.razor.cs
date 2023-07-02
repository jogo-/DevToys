﻿using DevToys.Blazor.Components.Monaco;
using DevToys.Blazor.Components.Monaco.Editor;

namespace DevToys.Blazor.Components.UIElements;

public partial class UIDiffTextInputPresenter : JSStyledComponentBase, IDisposable
{
    private MonacoEditorDiff _monacoEditor = default!;
    private bool _isInFullScreenMode;

    [Parameter]
    public IUIDiffTextInput UIDiffTextInput { get; set; } = default!;

    [CascadingParameter]
    protected FullScreenContainer? FullScreenContainer { get; set; }

    protected string ExtendedId => UIDiffTextInput.Id + "-" + Id;

    private string _originalModelName = string.Empty;
    private string _modifiedModelName = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _originalModelName = ExtendedId + "-originalModel";
        _modifiedModelName = ExtendedId + "-modifiedModel";

        UIDiffTextInput.TextChanged += UIMultiLineTextInput_TextChanged;
        UIDiffTextInput.ModifiedTextChanged += UIDiffTextInput_RightTextChanged;
        UIDiffTextInput.IsReadOnlyChanged += UIMultiLineTextInput_IsReadOnlyChanged;
        UIDiffTextInput.InlineModeChanged += UIDiffTextInput_InlineModeChanged;
    }

    public void Dispose()
    {
        UIDiffTextInput.TextChanged -= UIMultiLineTextInput_TextChanged;
        UIDiffTextInput.ModifiedTextChanged -= UIDiffTextInput_RightTextChanged;
        UIDiffTextInput.IsReadOnlyChanged -= UIMultiLineTextInput_IsReadOnlyChanged;
        UIDiffTextInput.InlineModeChanged -= UIDiffTextInput_InlineModeChanged;
        GC.SuppressFinalize(this);
    }

    private async Task OnMonacoEditorInitializedAsync()
    {
        // Get or create the original model
        TextModel original_model = await MonacoEditorHelper.GetModelAsync(JSRuntime, uri: _originalModelName);
        if (original_model == null)
        {
            original_model
                = await MonacoEditorHelper.CreateModelAsync(
                    JSRuntime,
                    UIDiffTextInput.Text,
                    language: null,
                    uri: _originalModelName);
        }

        // Get or create the modified model
        TextModel modified_model = await MonacoEditorHelper.GetModelAsync(JSRuntime, uri: _modifiedModelName);
        if (modified_model == null)
        {
            modified_model
                = await MonacoEditorHelper.CreateModelAsync(
                    JSRuntime,
                    UIDiffTextInput.ModifiedText,
                    language: null,
                    uri: _modifiedModelName);
        }

        // Set the editor model
        await _monacoEditor.SetModelAsync(new DiffEditorModel
        {
            Original = original_model,
            Modified = modified_model
        });

        Guard.IsNotNull(_monacoEditor.ModifiedEditor);
        await _monacoEditor.ModifiedEditor.UpdateOptionsAsync(
            new EditorUpdateOptions
            {
                ReadOnly = UIDiffTextInput.IsReadOnly
            });
    }

    private async void UIMultiLineTextInput_TextChanged(object? sender, EventArgs e)
    {
        Guard.IsNotNull(_monacoEditor.OriginalEditor);
        await _monacoEditor.OriginalEditor.SetValueAsync(UIDiffTextInput.Text);
    }

    private async void UIDiffTextInput_RightTextChanged(object? sender, EventArgs e)
    {
        Guard.IsNotNull(_monacoEditor.ModifiedEditor);
        await _monacoEditor.ModifiedEditor.SetValueAsync(UIDiffTextInput.ModifiedText);
    }

    private async void UIMultiLineTextInput_IsReadOnlyChanged(object? sender, EventArgs e)
    {
        Guard.IsNotNull(_monacoEditor.ModifiedEditor);
        await _monacoEditor.UpdateOptionsAsync(
            new DiffEditorOptions
            {
                ReadOnly = UIDiffTextInput.IsReadOnly,
                OriginalEditable = !UIDiffTextInput.IsReadOnly
            });
        await _monacoEditor.ModifiedEditor.UpdateOptionsAsync(
            new EditorUpdateOptions
            {
                ReadOnly = UIDiffTextInput.IsReadOnly
            });
    }

    private async void UIDiffTextInput_InlineModeChanged(object? sender, EventArgs e)
    {
        await _monacoEditor.UpdateOptionsAsync(
            new DiffEditorOptions
            {
                EnableSplitViewResizing = !UIDiffTextInput.InlineMode,
                RenderSideBySide = !UIDiffTextInput.InlineMode
            });
    }

    private async Task OnToggleFullScreenButtonClickAsync()
    {
        Guard.IsNotNull(FullScreenContainer);
        _isInFullScreenMode = await FullScreenContainer.ToggleFullScreenModeAsync(ExtendedId);
        StateHasChanged();
    }

    private StandaloneDiffEditorConstructionOptions OnMonacoConstructionOptions(MonacoEditorDiff monacoEditor)
    {
        return new StandaloneDiffEditorConstructionOptions
        {
            ReadOnly = UIDiffTextInput.IsReadOnly,
            OriginalEditable = !UIDiffTextInput.IsReadOnly,
            EnableSplitViewResizing = !UIDiffTextInput.InlineMode,
            RenderSideBySide = !UIDiffTextInput.InlineMode,
            AutomaticLayout = true
        };
    }
}
