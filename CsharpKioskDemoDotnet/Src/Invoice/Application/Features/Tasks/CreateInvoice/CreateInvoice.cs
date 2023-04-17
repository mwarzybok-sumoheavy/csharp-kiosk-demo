using BitPay.Exceptions;
using CsharpKioskDemoDotnet.Invoice.Domain;
using CsharpKioskDemoDotnet.Shared.Infrastructure;
using CsharpKioskDemoDotnet.Shared.Logger;
using ILogger = CsharpKioskDemoDotnet.Shared.Logger.ILogger;

namespace CsharpKioskDemoDotnet.Invoice.Application.Features.Tasks.CreateInvoice;

public class CreateInvoice
{
    private readonly GetValidatedParams _getValidatedParams;
    private readonly CreateBitPayInvoice _createBitPayInvoice;
    private readonly InvoiceFactory _invoiceFactory;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger _logger;

    public CreateInvoice(
        GetValidatedParams getValidatedParams,
        CreateBitPayInvoice createBitPayInvoice,
        InvoiceFactory invoiceFactory,
        IInvoiceRepository invoiceRepository,
        ILogger logger
    )
    {
        _getValidatedParams = getValidatedParams;
        _createBitPayInvoice = createBitPayInvoice;
        _invoiceFactory = invoiceFactory;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    internal Domain.Invoice Execute(Dictionary<string, string?> requestParameters)
    {
        try
        {
            var validatedParams = _getValidatedParams.Execute(requestParameters);
            var uuid = Guid.NewGuid().ToString();
            var bitPayInvoice = _createBitPayInvoice.Execute(validatedParams, uuid);
            // new ObjectToJsonConverter().Execute(bitPayInvoice);
            var invoice = _invoiceFactory.Create(bitPayInvoice, uuid);

            _invoiceRepository.Save(invoice);

            _logger.Info(
                LogCode.INVOICE_CREATE_SUCCESS,
                "Successfully created invoice",
                new Dictionary<string, object?>
                {
                    { "id", invoice.Id }
                }
            );

            return invoice;
        }
        catch (InvoiceCreationException exception)
        {
            LogException(exception);
            throw new Exception(exception.InnerException!.Message);
        }
        catch (Exception exception)
        {
            LogException(exception);
            throw;
        }
    }

    private void LogException(Exception exception)
    {
        _logger.Error(
            LogCode.INVOICE_CREATE_FAIL,
            "Failed to create invoice",
            new Dictionary<string, object?>
            {
                { "errorMessage", exception.Message },
                { "stackTrace", exception.StackTrace }
            }
        );
    }
}