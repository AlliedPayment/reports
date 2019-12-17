
const sql = require('mssql'); // https://github.com/tediousjs/node-mssql#readme
const AWS = require('aws-sdk');
AWS.config.update({region: 'us-east-1'});
AWS.config.logger = console;
 
let response;

var errHandler = function(err) {
  console.log(err);
}

exports.lambdaHandler = async (event, context) => {
  var sqlDB="mssql://FinalSystem:gnCeUWSzqw8eFMkgUBGrr2ES4qzGQs@172.30.20.12?database=Final&encrypt=true"; 
  sqlDB="mssql://ProdSystem:QAq5z%3FYdpXzLVnBFshCX%3FecCPUmLMk@172.16.6.10?database=BillPayProd&encrypt=true&Trusted_Connection=False&Min Pool Size=10&TrustServerCertificate=True"; 
    try {
        response = { 'statusCode': 403 }
        var bucketName = process.env.DESTINATION_BUCKET;
        var AWSS3 = new AWS.S3({apiVersion: '2006-03-01'})
  
        var json='(null)';
        try {
          await sql.connect(sqlDB);
          const result = await sql.query("SELECT p.ShardKey, u.UserName as 'Username', c.FullName as 'Customer Name', al.CreateOn as 'Create On', p.Id, p.ConfirmationNumber, p.Amount, p.PayFromAccount, al.VerifiedThrough as 'Pay From Account Verification', p.PayFromRtn, p.PayToBankAccount, p.PayToBankRoutingNumber FROM Payments p FULL OUTER JOIN Users u ON u.Id = p.UserId FULL OUTER JOIN Customers c ON c.Id = p.Customer_Id FULL OUTER JOIN A2AAuditLog al ON al.PaymentTemplateId = p.PaymentTemplateId WHERE p.AchPattern = 'A2A' and p.Amount > 250 and GETDATE() <= DATEADD(DAY, 30, al.CreateOn)")
          json=JSON.stringify(result.recordset);
          json=json.length>0?json:'(null)';
        } catch(err) {
          console.error("mssql catch:",err, err.stack);y
        }


        var now = new Date();
        var logfile_name = now.getFullYear()+'/'+(now.getMonth()+1)+'/fraud-' + now.getFullYear() + "-"+ (now.getMonth()+1) + "-" + now.getDate() +'.json'
        var objectParams = {Bucket: bucketName, Key: logfile_name, Body: json};
        console.log("LOG: "+JSON.stringify(objectParams));
        var uploadPromise = AWSS3.putObject(objectParams).promise();
        uploadPromise.then(
          function(data) {
            console.log("Successfully uploaded data to " + bucketName + "/" + logfile_name);
            response = { 'statusCode': 200 }
          }).catch(
          function(err) {
            console.log("upload failed:",JSON.stringify(err));
            console.error("upload failed:",JSON.stringify(err));
            return err;
          }).then(function(data) {
            console.log(data);
        }, errHandler);
        await uploadPromise;
      } catch (err) {
       console.log("s3 catch(",bucketName,"):",err);
       return err;
     }    
    return response
};
